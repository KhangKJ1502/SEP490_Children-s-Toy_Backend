using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Background Service định kỳ gửi thông báo nhắc nhở khách hàng về các voucher sắp hết hạn trong vòng 3 ngày tới.
/// Lịch chạy: Chạy vào lúc 09:00 sáng hàng ngày (theo giờ Việt Nam).
/// Tự động bỏ qua các voucher hoặc người dùng đã hết lượt sử dụng.
/// </summary>
public class VoucherExpiryReminderJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<VoucherExpiryReminderJob> _logger;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Khởi tạo background job nhắc nhở voucher hết hạn.
    /// </summary>
    /// <param name="services">Service provider dùng để tạo scope nạp các service scoped (DbContext, Dispatcher,...).</param>
    /// <param name="logger">Logger ghi log thực thi của background worker.</param>
    /// <param name="timeProvider">Provider cung cấp thời gian thực hiện tại.</param>
    public VoucherExpiryReminderJob(
        IServiceProvider services, 
        ILogger<VoucherExpiryReminderJob> logger,
        ITimeProvider timeProvider)
    {
        _services     = services;
        _logger       = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Vòng lặp thực thi chính của BackgroundService, tính toán thời gian delay đến 09:00 sáng tiếp theo để kích hoạt RunAsync.
    /// </summary>
    /// <param name="stoppingToken">Token hủy tiến trình worker khi ứng dụng dừng.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now  = _timeProvider.VnNow;
            var next = now.Date.AddHours(9);
            // Nếu đã quá 09:00 hôm nay, đặt lịch cho 09:00 sáng ngày mai
            if (now.Hour >= 9) next = next.AddDays(1);

            // Chờ đến mốc 09:00 tiếp theo
            await Task.Delay(next - now, stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            // Thực thi gửi thông báo nhắc nhở
            await RunAsync(stoppingToken);
        }
    }

    /// <summary>
    /// Logic nghiệp vụ quét voucher sắp hết hạn và phát thông báo qua NotificationDispatcher.
    /// </summary>
    /// <param name="ct">Token hủy tác vụ bất đồng bộ.</param>
    private async Task RunAsync(CancellationToken ct)
    {
        using var scope  = _services.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var dispatcher   = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
        var prefChecker  = scope.ServiceProvider.GetRequiredService<IUserPreferenceChecker>();

        bool success = true;
        string? message = null;

        try
        {
            var now         = _timeProvider.UtcNow;
            var cutoffStart = now;
            var cutoffEnd   = now.AddDays(3); // Giới hạn trong vòng 3 ngày tới

            // 1. Tìm các voucher đang Active và có hạn dùng kết thúc trong vòng 3 ngày tới
            var expiringVouchers = await db.Vouchers
                .Where(v => v.Status == "Active"
                         && v.EndDate >= cutoffStart
                         && v.EndDate <= cutoffEnd)
                .ToListAsync(ct);

            int count = 0;
            foreach (var voucher in expiringVouchers)
            {
                // 2. Tìm danh sách tài khoản đã từng sử dụng voucher này nhưng chưa dùng hết số lần cho phép (MaxUsagePerUser)
                var eligibleAccountIds = await db.VoucherUsageLogs
                    .Where(l => l.VoucherId == voucher.VoucherId)
                    .GroupBy(l => l.AccountId)
                    .Where(g => g.Count() < voucher.MaxUsagePerUser)
                    .Select(g => g.Key)
                    .ToListAsync(ct);

                // 3. Gửi thông báo đến từng tài khoản hợp lệ
                foreach (var accountId in eligibleAccountIds)
                {
                    // Kiểm tra cài đặt nhận thông báo khuyến mãi của khách hàng
                    if (!await prefChecker.CanSendAsync(accountId, PreferenceKeys.Promotions, ct))
                        continue;

                    // Phát thông báo kèm idempotency key chống gửi trùng lặp trong ngày
                    await dispatcher.DispatchAsync(new NotificationContext
                    {
                        RecipientAccountId = accountId,
                        RecipientType      = RecipientTypes.Customer,
                        NotificationType   = NotificationTypes.Promotion,
                        TemplateCode       = NotificationTemplates.VoucherExpiring,
                        Placeholders       = new Dictionary<string, string>
                        {
                            ["VoucherCode"]  = voucher.VoucherCode,
                            ["DiscountValue"] = voucher.DiscountType == "PERCENTAGE"
                                                ? $"{voucher.DiscountValue:0.##}%"
                                                : $"{voucher.DiscountValue:N0} VND",
                            ["ExpiryDate"]   = voucher.EndDate.ToString("dd/MM/yyyy"),
                        },
                        IdempotencyKey = $"voucher.expiring:{voucher.VoucherId}:{accountId}:{now:yyyyMMdd}:WEB_BELL",
                        SendBell       = true,
                        SendEmail      = false,
                    }, ct);

                    count++;
                }
            }

            message = $"Sent {count} voucher expiry reminders";
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "VoucherExpiryReminderJob failed");
        }

        // 4. Ghi telemetry nhật ký thực thi của background job
        await BackgroundJobTelemetry.RecordAsync(db, "VoucherExpiryReminderJob", success, message, _logger, ct);
    }
}
