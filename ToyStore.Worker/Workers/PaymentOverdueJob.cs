using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Scans orders pending payment for more than 24h and alerts staff.
/// Runs every 15 minutes.
/// </summary>
public class PaymentOverdueJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PaymentOverdueJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(15);

    public PaymentOverdueJob(
        IServiceProvider services, 
        ILogger<PaymentOverdueJob> logger,
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
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "PaymentOverdueJob error"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var dispatcher  = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

        bool success = true;
        string? message = null;

        try
        {
            var cutoff = _timeProvider.UtcNow.AddHours(-24);

            // Orders with StatusID=1 (Pending), older than 24h, without a PAID payment
            var overdueOrders = await db.Orders
                .Where(o => o.StatusId == 1
                         && !o.IsDeleted
                         && o.CreatedAt < cutoff
                         && !o.PaymentHistories.Any(ph => ph.PaymentStatus == "PAID"))
                .Select(o => new { o.OrderId, o.OrderCode, o.AccountId })
                .ToListAsync(ct);

            var staffAccounts = await db.Accounts
                .Where(a => a.Role.RoleName == "Staff" && a.IsActive && !a.IsDeleted)
                .Select(a => a.AccountId)
                .ToListAsync(ct);

            foreach (var order in overdueOrders)
            {
                foreach (var staffId in staffAccounts)
                {
                    await dispatcher.DispatchAsync(new NotificationContext
                    {
                        RecipientAccountId = staffId,
                        RecipientType      = RecipientTypes.Staff,
                        NotificationType   = NotificationTypes.Order,
                        Title              = "Đơn quá hạn thanh toán",
                        Message            = $"Đơn {order.OrderCode} chờ thanh toán quá 24 giờ",
                        SendBell           = true,
                        SendEmail          = false,
                        TemplateCode       = NotificationTemplates.StaffNewOrder,
                        IdempotencyKey     = $"order.payment_overdue:{order.OrderId}:{_timeProvider.UtcNow:yyyyMMddHH}:{staffId}",
                    }, ct);
                }
            }

            message = $"Overdue orders alerted: {overdueOrders.Count}";
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "PaymentOverdueJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(db, "PaymentOverdueJob", success, message, _logger, ct);
    }
}
