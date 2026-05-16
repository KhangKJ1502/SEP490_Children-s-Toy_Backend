using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Notifies customers about vouchers expiring within the next 3 days.
/// Runs at 09:00 daily. Skips vouchers already fully used.
/// </summary>
public class VoucherExpiryReminderJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<VoucherExpiryReminderJob> _logger;
    private readonly ITimeProvider _timeProvider;

    public VoucherExpiryReminderJob(
        IServiceProvider services, 
        ILogger<VoucherExpiryReminderJob> logger,
        ITimeProvider timeProvider)
    {
        _services     = services;
        _logger       = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now  = _timeProvider.VnNow;
            var next = now.Date.AddHours(9);
            if (now.Hour >= 9) next = next.AddDays(1);

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

        bool success = true;
        string? message = null;

        try
        {
            var now         = _timeProvider.UtcNow;
            var cutoffStart = now;
            var cutoffEnd   = now.AddDays(3);

            // Vouchers expiring within 3 days, still active
            var expiringVouchers = await db.Vouchers
                .Where(v => v.Status == "Active"
                         && v.EndDate >= cutoffStart
                         && v.EndDate <= cutoffEnd)
                .ToListAsync(ct);

            int count = 0;
            foreach (var voucher in expiringVouchers)
            {
                // Find accounts that own this voucher but haven't exhausted their usage
                var eligibleAccountIds = await db.VoucherUsageLogs
                    .Where(l => l.VoucherId == voucher.VoucherId)
                    .GroupBy(l => l.AccountId)
                    .Where(g => g.Count() < voucher.MaxUsagePerUser)
                    .Select(g => g.Key)
                    .ToListAsync(ct);

                foreach (var accountId in eligibleAccountIds)
                {
                    if (!await prefChecker.CanSendAsync(accountId, PreferenceKeys.Promotions, ct))
                        continue;

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

        await BackgroundJobTelemetry.RecordAsync(db, "VoucherExpiryReminderJob", success, message, _logger, ct);
    }
}
