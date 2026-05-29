using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Sends birthday greetings at 08:00 daily to customers and their registered children.
/// Customer deduplication: IdempotencyKey on Deliveries.
/// Child deduplication: BirthdayNotifiedYear on CustomerChildren (set to current year after send).
/// </summary>
public class BirthdayNotificationJob : BackgroundService
{
    private const int PreBirthdayDays = 7;

    private readonly IServiceProvider _services;
    private readonly ILogger<BirthdayNotificationJob> _logger;
    private readonly ITimeProvider _timeProvider;

    public BirthdayNotificationJob(
        IServiceProvider services,
        ILogger<BirthdayNotificationJob> logger,
        ITimeProvider timeProvider)
    {
        _services = services;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BirthdayNotificationJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = _timeProvider.VnNow;
            var next = now.Date.AddHours(8);
            if (now.Hour >= 8) next = next.AddDays(1);

            await Task.Delay(next - now, stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
        var prefChecker = scope.ServiceProvider.GetRequiredService<IUserPreferenceChecker>();

        bool success = true;
        string? errMsg = null;

        try
        {
            var today = DateOnly.FromDateTime(_timeProvider.VnNow);
            var year = (short)today.Year;
            var preBirthdayDate = today.AddDays(PreBirthdayDays);
            var preBirthdayYear = (short)preBirthdayDate.Year;

            var isTodayFeb28NonLeap = IsFeb28NonLeap(today);
            var isPreFeb28NonLeap = IsFeb28NonLeap(preBirthdayDate);
            var hasMissing31FallbackToday = TryGetMissing31FallbackMonth(today, out var missing31MonthToday);
            var hasMissing31FallbackPre = TryGetMissing31FallbackMonth(preBirthdayDate, out var missing31MonthPre);

            // ── Customer birthdays ────────────────────────────────────────────
            // IdempotencyKey prevents re-sending in case of job retry; no BirthdayNotifiedYear on Account
            var accountBirthdays = await db.Accounts
                .Where(a => a.Dob != null
                         && ((a.Dob.Value.Day == today.Day
                              && a.Dob.Value.Month == today.Month)
                             || (a.Dob.Value.Month == 2
                                 && a.Dob.Value.Day == 29
                                 && isTodayFeb28NonLeap)
                             || (hasMissing31FallbackToday
                                 && a.Dob.Value.Month == missing31MonthToday
                                 && a.Dob.Value.Day == 31))
                         && a.IsActive
                         && !a.IsDeleted)
                .ToListAsync(ct);

            var accountPreBirthdays = await db.Accounts
                .Where(a => a.Dob != null
                         && ((a.Dob.Value.Day == preBirthdayDate.Day
                              && a.Dob.Value.Month == preBirthdayDate.Month)
                             || (a.Dob.Value.Month == 2
                                 && a.Dob.Value.Day == 29
                                 && isPreFeb28NonLeap)
                             || (hasMissing31FallbackPre
                                 && a.Dob.Value.Month == missing31MonthPre
                                 && a.Dob.Value.Day == 31))
                         && a.IsActive
                         && !a.IsDeleted)
                .ToListAsync(ct);

            foreach (var account in accountBirthdays)
            {
                if (!await prefChecker.CanSendAsync(account.AccountId, PreferenceKeys.Promotions, ct))
                    continue;

                await dispatcher.DispatchAsync(new NotificationContext
                {
                    RecipientAccountId = account.AccountId,
                    RecipientType = RecipientTypes.Customer,
                    NotificationType = NotificationTypes.Promotion,
                    TemplateCode = NotificationTemplates.BirthdayCustomer,
                    Placeholders = new Dictionary<string, string>
                    {
                        ["CustomerName"] = account.AccountName,
                    },
                    IdempotencyKey = $"birthday.customer:{account.AccountId}:{year}:WEB_BELL",
                    SendBell = true,
                    SendEmail = true,
                }, ct);
            }

            foreach (var account in accountPreBirthdays)
            {
                if (!await prefChecker.CanSendAsync(account.AccountId, PreferenceKeys.Promotions, ct))
                    continue;

                await dispatcher.DispatchAsync(new NotificationContext
                {
                    RecipientAccountId = account.AccountId,
                    RecipientType = RecipientTypes.Customer,
                    NotificationType = NotificationTypes.Promotion,
                    TemplateCode = NotificationTemplates.BirthdayCustomer,
                    Placeholders = new Dictionary<string, string>
                    {
                        ["CustomerName"] = account.AccountName,
                    },
                    IdempotencyKey = $"birthday.customer.pre:{account.AccountId}:{preBirthdayYear}:WEB_BELL",
                    SendBell = true,
                    SendEmail = true,
                }, ct);
            }

            // ── Child birthdays ───────────────────────────────────────────────
            // Only send when BirthdayNotifiedYear IS NULL OR BirthdayNotifiedYear < current year
            var childBirthdays = await db.CustomerChildren
                .Include(c => c.Account)
                .Include(c => c.Sex)
                .Where(c => !c.IsDeleted
                         && ((c.Dob.Day == today.Day
                              && c.Dob.Month == today.Month)
                             || (c.Dob.Month == 2
                                 && c.Dob.Day == 29
                                 && isTodayFeb28NonLeap)
                             || (hasMissing31FallbackToday
                                 && c.Dob.Month == missing31MonthToday
                                 && c.Dob.Day == 31))
                         && c.Account.IsActive
                         && !c.Account.IsDeleted
                         && (c.BirthdayNotifiedYear == null || c.BirthdayNotifiedYear < year))
                .ToListAsync(ct);

            var childPreBirthdays = await db.CustomerChildren
                .Include(c => c.Account)
                .Include(c => c.Sex)
                .Where(c => !c.IsDeleted
                         && ((c.Dob.Day == preBirthdayDate.Day
                              && c.Dob.Month == preBirthdayDate.Month)
                             || (c.Dob.Month == 2
                                 && c.Dob.Day == 29
                                 && isPreFeb28NonLeap)
                             || (hasMissing31FallbackPre
                                 && c.Dob.Month == missing31MonthPre
                                 && c.Dob.Day == 31))
                         && c.Account.IsActive
                         && !c.Account.IsDeleted)
                .ToListAsync(ct);

            foreach (var child in childBirthdays)
            {
                if (!await prefChecker.CanSendAsync(child.AccountId, PreferenceKeys.Promotions, ct))
                    continue;

                var childName = string.IsNullOrWhiteSpace(child.NickName) ? child.FullName : child.NickName;
                var childNickname = string.IsNullOrWhiteSpace(child.NickName) ? string.Empty : child.NickName;
                var childGender = child.Sex?.SexName
                    ?? (child.SexId == 1 ? "Boy" : child.SexId == 2 ? "Girl" : string.Empty);
                var childBirthDate = child.Dob.ToString("dd/MM/yyyy");

                await dispatcher.DispatchAsync(new NotificationContext
                {
                    RecipientAccountId = child.AccountId,
                    RecipientType = RecipientTypes.Customer,
                    NotificationType = NotificationTypes.Promotion,
                    TemplateCode = NotificationTemplates.BirthdayChild,
                    Placeholders = new Dictionary<string, string>
                    {
                        ["ChildName"] = childName,
                    },
                    IdempotencyKey = $"birthday.child:{child.ChildId}:{year}:WEB_BELL",
                    SendBell = true,
                    SendEmail = true,
                    Payload = new Dictionary<string, object>
                    {
                        ["childId"] = child.ChildId,
                        ["childName"] = childName,
                        ["childNickname"] = childNickname,
                        ["childGender"] = childGender,
                        ["childBirthDate"] = childBirthDate,
                    },
                }, ct);

                // Mark as sent for this year
                child.BirthdayNotifiedYear = year;
                child.UpdatedAt = _timeProvider.UtcNow;
            }

            foreach (var child in childPreBirthdays)
            {
                if (!await prefChecker.CanSendAsync(child.AccountId, PreferenceKeys.Promotions, ct))
                    continue;

                var childName = string.IsNullOrWhiteSpace(child.NickName) ? child.FullName : child.NickName;
                var childNickname = string.IsNullOrWhiteSpace(child.NickName) ? string.Empty : child.NickName;
                var childGender = child.Sex?.SexName
                    ?? (child.SexId == 1 ? "Boy" : child.SexId == 2 ? "Girl" : string.Empty);
                var childBirthDate = child.Dob.ToString("dd/MM/yyyy");

                await dispatcher.DispatchAsync(new NotificationContext
                {
                    RecipientAccountId = child.AccountId,
                    RecipientType = RecipientTypes.Customer,
                    NotificationType = NotificationTypes.Promotion,
                    TemplateCode = NotificationTemplates.BirthdayChild,
                    Placeholders = new Dictionary<string, string>
                    {
                        ["ChildName"] = childName,
                    },
                    IdempotencyKey = $"birthday.child.pre:{child.ChildId}:{preBirthdayYear}:WEB_BELL",
                    SendBell = true,
                    SendEmail = true,
                    Payload = new Dictionary<string, object>
                    {
                        ["childId"] = child.ChildId,
                        ["childName"] = childName,
                        ["childNickname"] = childNickname,
                        ["childGender"] = childGender,
                        ["childBirthDate"] = childBirthDate,
                    },
                }, ct);
            }

            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "BirthdayNotificationJob: {AccountCount} accounts, {AccountPreCount} account pre-birthdays, {ChildCount} children, {ChildPreCount} child pre-birthdays processed",
                accountBirthdays.Count, accountPreBirthdays.Count, childBirthdays.Count, childPreBirthdays.Count);

            errMsg = $"Accounts: {accountBirthdays.Count}, AccountPre: {accountPreBirthdays.Count}, Children: {childBirthdays.Count}, ChildPre: {childPreBirthdays.Count}";
        }
        catch (Exception ex)
        {
            success = false;
            errMsg = ex.Message;
            _logger.LogError(ex, "BirthdayNotificationJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(db, "BirthdayNotificationJob", success, errMsg, _logger, ct);
    }

    private static bool IsFeb28NonLeap(DateOnly date)
    {
        return date.Month == 2 && date.Day == 28 && !DateTime.IsLeapYear(date.Year);
    }

    private static bool TryGetMissing31FallbackMonth(DateOnly targetDate, out int fallbackMonth)
    {
        fallbackMonth = 0;
        if (targetDate.Day != 1)
            return false;

        var previousDay = targetDate.AddDays(-1);
        if (previousDay.Day == 31)
            return false;

        fallbackMonth = previousDay.Month;
        return true;
    }
}
