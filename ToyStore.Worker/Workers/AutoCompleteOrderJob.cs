using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
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
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public AutoCompleteOrderJob(
        IServiceProvider services,
        ILogger<AutoCompleteOrderJob> logger,
        ITimeProvider timeProvider)
    {
        _services = services;
        _logger = logger;
        _timeProvider = timeProvider;
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
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
        var lifecycleService = scope.ServiceProvider.GetRequiredService<IOrderLifecycleService>();

        bool success = true;
        string? message = null;

        try
        {
            var cutoff = _timeProvider.UtcNow.AddDays(-7);

            // StatusID=6 (Delivered), delivered more than 7 days ago, customer hasn't confirmed
            var ordersToComplete = await db.Orders
                .Where(o => o.StatusId == 6
                         && !o.IsDeleted
                         && o.DeliveredAt != null
                         && o.DeliveredAt < cutoff)
                .ToListAsync(ct);

            foreach (var order in ordersToComplete)
            {
                // Gọi lifecycle service để xử lý status + release capacity
                var result = await lifecycleService.CompleteOrderAsync(order.OrderId, ct);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to complete order {OrderId} in job: {Error}", order.OrderId, result.ErrorMessage);
                    continue;
                }

                // Bell-only notification
                await dispatcher.DispatchAsync(new NotificationContext
                {
                    RecipientAccountId = order.AccountId,
                    RecipientType = RecipientTypes.Customer,
                    NotificationType = NotificationTypes.Order,
                    Title = "Đơn hàng đã hoàn thành",
                    Message = $"Đơn {order.OrderCode} đã được tự động hoàn thành sau 7 ngày nhận hàng",
                    SendBell = true,
                    SendEmail = false,
                    TemplateCode = NotificationTemplates.OrderDelivered,
                    ActionTarget = $"/orders/{order.OrderId}",
                    IdempotencyKey = $"order.completed:auto:{order.OrderId}",
                }, ct);
            }

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
