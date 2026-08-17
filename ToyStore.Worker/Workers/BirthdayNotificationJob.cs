using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Gửi lời chúc sinh nhật lúc 08:00 mỗi ngày tới khách hàng và trẻ em đã đăng ký.
/// Khách hàng: chống gửi trùng bằng IdempotencyKey trên bảng Deliveries.
/// Trẻ em:     chống gửi trùng bằng BirthdayNotifiedYear trên CustomerChildren (cập nhật sau khi gửi).
/// Ngoài ra gửi thông báo trước 7 ngày (pre-birthday) để nhắc phụ huynh chuẩn bị.
/// </summary>
public class BirthdayNotificationJob : BackgroundService
{
    // Số ngày nhắc trước sinh nhật
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
        _logger.LogInformation("BirthdayNotificationJob khởi động");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = _timeProvider.VnNow;

            // Tính thời điểm chạy tiếp theo: 08:00 hôm nay nếu chưa tới, hoặc 08:00 ngày mai
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

            // Ngày nhắc trước (pre-birthday = hôm nay + 7 ngày)
            var preBirthdayDate = today.AddDays(PreBirthdayDays);
            var preBirthdayYear = (short)preBirthdayDate.Year;

            // Xử lý edge case: 28/02 năm thường thay thế cho 29/02
            var isTodayFeb28NonLeap = IsFeb28NonLeap(today);
            var isPreFeb28NonLeap   = IsFeb28NonLeap(preBirthdayDate);

            // Xử lý edge case: ngày 01 đầu tháng thay thế cho ngày 31 tháng trước nếu tháng đó không có ngày 31
            var hasMissing31FallbackToday = TryGetMissing31FallbackMonth(today, out var missing31MonthToday);
            var hasMissing31FallbackPre   = TryGetMissing31FallbackMonth(preBirthdayDate, out var missing31MonthPre);

            // ── Sinh nhật khách hàng (Account) ───────────────────────────────
            // IdempotencyKey trên Deliveries đảm bảo không gửi trùng khi job retry
            var accountBirthdays = await db.Accounts
                .Where(a => a.Dob != null
                         && ((a.Dob.Value.Day == today.Day
                               && a.Dob.Value.Month == today.Month)
                             || (a.Dob.Value.Month == 2          // Sinh nhật 29/02 → gửi vào 28/02 năm thường
                                 && a.Dob.Value.Day == 29
                                 && isTodayFeb28NonLeap)
                             || (hasMissing31FallbackToday        // Sinh nhật 31 → gửi vào 01 tháng sau nếu tháng không có 31
                                 && a.Dob.Value.Month == missing31MonthToday
                                 && a.Dob.Value.Day == 31))
                         && a.IsActive
                         && !a.IsDeleted)
                .ToListAsync(ct);

            // Nhắc trước sinh nhật 7 ngày cho khách hàng
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

            // Gửi chúc mừng sinh nhật cho khách hàng (hôm nay)
            foreach (var account in accountBirthdays)
            {
                // Kiểm tra tùy chọn nhận thông báo khuyến mãi của người dùng
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
                    // Key duy nhất theo AccountId + năm để tránh gửi trùng
                    IdempotencyKey = $"birthday.customer:{account.AccountId}:{year}:WEB_BELL",
                    SendBell  = true,
                    SendEmail = true,
                }, ct);
            }

            // Gửi nhắc trước sinh nhật 7 ngày cho khách hàng
            foreach (var account in accountPreBirthdays)
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
                    IdempotencyKey = $"birthday.customer.pre:{account.AccountId}:{preBirthdayYear}:WEB_BELL",
                    SendBell  = true,
                    SendEmail = true,
                }, ct);
            }

            // ── Sinh nhật trẻ em (CustomerChildren) ──────────────────────────
            // Chỉ gửi khi BirthdayNotifiedYear chưa được set hoặc nhỏ hơn năm hiện tại
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

            // BUG FIX: Thiếu filter BirthdayNotifiedYear cho pre-birthday trẻ em
            // → Không filter sẽ gửi nhắc trước sinh nhật kể cả những trẻ đã được gửi trong năm nay
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
                         && !c.Account.IsDeleted
                         && (c.BirthdayNotifiedYear == null || c.BirthdayNotifiedYear < preBirthdayYear))
                .ToListAsync(ct);

            // Gửi chúc mừng sinh nhật cho trẻ em (hôm nay)
            foreach (var child in childBirthdays)
            {
                if (!await prefChecker.CanSendAsync(child.AccountId, PreferenceKeys.Promotions, ct))
                    continue;

                // Ưu tiên NickName, nếu không có dùng FullName
                var childName      = string.IsNullOrWhiteSpace(child.NickName) ? child.FullName : child.NickName;
                var childNickname  = string.IsNullOrWhiteSpace(child.NickName) ? string.Empty : child.NickName;
                var childGender    = child.Sex?.SexName
                    ?? (child.SexId == 1 ? "Boy" : child.SexId == 2 ? "Girl" : string.Empty);
                var childBirthDate = child.Dob.ToString("dd/MM/yyyy");

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
                    SendBell  = true,
                    SendEmail = true,
                    Payload = new Dictionary<string, object>
                    {
                        ["childId"]        = child.ChildId,
                        ["childName"]      = childName,
                        ["childNickname"]  = childNickname,
                        ["childGender"]    = childGender,
                        ["childBirthDate"] = childBirthDate,
                    },
                }, ct);

                // Đánh dấu đã gửi trong năm nay để tránh gửi trùng khi job chạy lại
                child.BirthdayNotifiedYear = year;
                child.UpdatedAt = _timeProvider.UtcNow;
            }

            // Gửi nhắc trước sinh nhật 7 ngày cho trẻ em
            foreach (var child in childPreBirthdays)
            {
                if (!await prefChecker.CanSendAsync(child.AccountId, PreferenceKeys.Promotions, ct))
                    continue;

                var childName      = string.IsNullOrWhiteSpace(child.NickName) ? child.FullName : child.NickName;
                var childNickname  = string.IsNullOrWhiteSpace(child.NickName) ? string.Empty : child.NickName;
                var childGender    = child.Sex?.SexName
                    ?? (child.SexId == 1 ? "Boy" : child.SexId == 2 ? "Girl" : string.Empty);
                var childBirthDate = child.Dob.ToString("dd/MM/yyyy");

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
                    IdempotencyKey = $"birthday.child.pre:{child.ChildId}:{preBirthdayYear}:WEB_BELL",
                    SendBell  = true,
                    SendEmail = true,
                    Payload = new Dictionary<string, object>
                    {
                        ["childId"]        = child.ChildId,
                        ["childName"]      = childName,
                        ["childNickname"]  = childNickname,
                        ["childGender"]    = childGender,
                        ["childBirthDate"] = childBirthDate,
                    },
                }, ct);
            }

            // Lưu cập nhật BirthdayNotifiedYear cho tất cả trẻ đã gửi
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "BirthdayNotificationJob: {AccountCount} tài khoản, {AccountPreCount} tài khoản nhắc trước, {ChildCount} trẻ em, {ChildPreCount} trẻ em nhắc trước",
                accountBirthdays.Count, accountPreBirthdays.Count, childBirthdays.Count, childPreBirthdays.Count);

            errMsg = $"Accounts: {accountBirthdays.Count}, AccountPre: {accountPreBirthdays.Count}, Children: {childBirthdays.Count}, ChildPre: {childPreBirthdays.Count}";
        }
        catch (Exception ex)
        {
            success = false;
            errMsg  = ex.Message;
            _logger.LogError(ex, "BirthdayNotificationJob thất bại");
        }

        await BackgroundJobTelemetry.RecordAsync(db, "BirthdayNotificationJob", success, errMsg, _logger, ct);
    }

    /// <summary>
    /// Kiểm tra xem ngày đã cho có phải 28/02 của năm không nhuận hay không.
    /// Dùng để gửi sinh nhật cho người sinh ngày 29/02 vào năm thường.
    /// </summary>
    private static bool IsFeb28NonLeap(DateOnly date)
    {
        return date.Month == 2 && date.Day == 28 && !DateTime.IsLeapYear(date.Year);
    }

    /// <summary>
    /// Kiểm tra xem ngày đã cho có phải ngày 01 của tháng mà tháng trước không có ngày 31 hay không.
    /// Ví dụ: 01/05 → fallback cho sinh nhật 31/04 (tháng 4 chỉ có 30 ngày).
    /// Trả về true và gán <paramref name="fallbackMonth"/> = tháng trước nếu thỏa điều kiện.
    /// </summary>
    private static bool TryGetMissing31FallbackMonth(DateOnly targetDate, out int fallbackMonth)
    {
        fallbackMonth = 0;

        // Chỉ xử lý khi hôm nay là ngày 01 của tháng
        if (targetDate.Day != 1)
            return false;

        var previousDay = targetDate.AddDays(-1);

        // Nếu ngày hôm qua là 31 thì tháng trước vốn có 31 ngày → không cần fallback
        if (previousDay.Day == 31)
            return false;

        fallbackMonth = previousDay.Month;
        return true;
    }
}
