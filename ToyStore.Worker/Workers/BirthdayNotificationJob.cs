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
    private readonly IServiceProvider _services;
    private readonly ILogger<BirthdayNotificationJob> _logger;
    private readonly ITimeProvider _timeProvider;

    public BirthdayNotificationJob(
        IServiceProvider services, 
        ILogger<BirthdayNotificationJob> logger,
        ITimeProvider timeProvider)
    {
        _services     = services;
        _logger       = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BirthdayNotificationJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now  = _timeProvider.VnNow;
            var next = now.Date.AddHours(8);
            if (now.Hour >= 8) next = next.AddDays(1);

            await Task.Delay(next - now, stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope  = _services.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var dispatcher   = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
        var prefChecker  = scope.ServiceProvider.GetRequiredService<IUserPreferenceChecker>();

        bool success   = true;
        string? errMsg = null;

        try
        {
            var today = DateOnly.FromDateTime(_timeProvider.VnNow);
            var year  = (short)today.Year;

            // ── Customer birthdays ────────────────────────────────────────────
            // IdempotencyKey prevents re-sending in case of job retry; no BirthdayNotifiedYear on Account
            var accountBirthdays = await db.Accounts
                .Where(a => a.Dob != null
                         && a.Dob.Value.Day   == today.Day
                         && a.Dob.Value.Month == today.Month
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
                    RecipientType      = RecipientTypes.Customer,
                    NotificationType   = NotificationTypes.Promotion,
                    TemplateCode       = NotificationTemplates.BirthdayCustomer,
                    Placeholders       = new Dictionary<string, string>
                    {
                        ["CustomerName"] = account.AccountName,
                    },
                    IdempotencyKey = $"birthday.customer:{account.AccountId}:{year}:WEB_BELL",
                    SendBell       = true,
                    SendEmail      = true,
                }, ct);
            }

            // ── Child birthdays ───────────────────────────────────────────────
            // Only send when BirthdayNotifiedYear IS NULL OR BirthdayNotifiedYear < current year
            var childBirthdays = await db.CustomerChildren
                .Include(c => c.Account)
                .Where(c => !c.IsDeleted
                         && c.Dob.Day   == today.Day
                         && c.Dob.Month == today.Month
                         && c.Account.IsActive
                         && !c.Account.IsDeleted
                         && (c.BirthdayNotifiedYear == null || c.BirthdayNotifiedYear < year))
                .ToListAsync(ct);

            foreach (var child in childBirthdays)
            {
                if (!await prefChecker.CanSendAsync(child.AccountId, PreferenceKeys.Promotions, ct))
                    continue;

                var childName = string.IsNullOrWhiteSpace(child.NickName) ? child.FullName : child.NickName;

                await dispatcher.DispatchAsync(new NotificationContext
                {
                    RecipientAccountId = child.AccountId,
                    RecipientType      = RecipientTypes.Customer,
                    NotificationType   = NotificationTypes.Promotion,
                    TemplateCode       = NotificationTemplates.BirthdayChild,
                    Placeholders       = new Dictionary<string, string>
                    {
                        ["ChildName"] = childName,
                    },
                    IdempotencyKey = $"birthday.child:{child.ChildId}:{year}:WEB_BELL",
                    SendBell       = true,
                    SendEmail      = true,
                    Payload        = new Dictionary<string, object>
                    {
                        ["childId"]   = child.ChildId,
                        ["childName"] = childName,
                    },
                }, ct);

                // Mark as sent for this year
                child.BirthdayNotifiedYear = year;
                child.UpdatedAt            = _timeProvider.UtcNow;
            }

            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "BirthdayNotificationJob: {AccountCount} accounts, {ChildCount} children processed",
                accountBirthdays.Count, childBirthdays.Count);

            errMsg = $"Accounts: {accountBirthdays.Count}, Children: {childBirthdays.Count}";
        }
        catch (Exception ex)
        {
            success = false;
            errMsg  = ex.Message;
            _logger.LogError(ex, "BirthdayNotificationJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(db, "BirthdayNotificationJob", success, errMsg, _logger, ct);
    }
}
