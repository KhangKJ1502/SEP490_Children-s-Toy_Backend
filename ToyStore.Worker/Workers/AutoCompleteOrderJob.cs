using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Auto-completes orders 7 days after delivery when the customer hasn't confirmed.
/// Sends a bell notification only (customer initiated their own confirm — no notify needed for that).
/// Runs every hour.
/// </summary>
public class AutoCompleteOrderJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AutoCompleteOrderJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public AutoCompleteOrderJob(IServiceProvider services, ILogger<AutoCompleteOrderJob> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "AutoCompleteOrderJob error"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope  = _services.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var dispatcher   = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

        bool success = true;
        string? message = null;

        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);

            // StatusID=6 (Delivered), delivered more than 7 days ago, customer hasn't confirmed
            var ordersToComplete = await db.Orders
                .Where(o => o.StatusId == 6
                         && !o.IsDeleted
                         && o.DeliveredAt != null
                         && o.DeliveredAt < cutoff)
                .ToListAsync(ct);

            foreach (var order in ordersToComplete)
            {
                order.StatusId    = 7; // Completed
                order.CompletedAt = DateTime.UtcNow;
                order.UpdatedAt   = DateTime.UtcNow;

                // Bell-only: auto-completion, customer didn't initiate
                await dispatcher.DispatchAsync(new NotificationContext
                {
                    RecipientAccountId = order.AccountId,
                    RecipientType      = RecipientTypes.Customer,
                    NotificationType   = NotificationTypes.Order,
                    Title              = "Đơn hàng đã hoàn thành",
                    Message            = $"Đơn {order.OrderCode} đã được tự động hoàn thành sau 7 ngày nhận hàng",
                    SendBell           = true,
                    SendEmail          = false,
                    TemplateCode       = NotificationTemplates.OrderDelivered,
                    ActionTarget       = $"/orders/{order.OrderId}",
                    IdempotencyKey     = $"order.completed:auto:{order.OrderId}",
                }, ct);
            }

            await db.SaveChangesAsync(ct);
            message = $"Auto-completed {ordersToComplete.Count} orders";
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "AutoCompleteOrderJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(db, "AutoCompleteOrderJob", success, message, _logger, ct);
    }
}
