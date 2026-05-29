using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Background worker xử lý tự động các đơn hàng pending:
/// - Huỷ đơn chưa thanh toán sau 24h
/// - Gọi SP_SyncPromotionVoucherStatus để đồng bộ status
/// </summary>
public class OrderStatusWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderStatusWorker> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public OrderStatusWorker(
        IServiceProvider serviceProvider,
        ILogger<OrderStatusWorker> logger,
        ITimeProvider timeProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order Status Worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending orders");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Order Status Worker stopping");
    }

    private async Task ProcessPendingOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var lifecycleService = scope.ServiceProvider.GetRequiredService<IOrderLifecycleService>();

        _logger.LogInformation("Checking for pending orders to auto-cancel");

        // Huỷ đơn PENDING chưa thanh toán sau 24h
        var cutoff = _timeProvider.UtcNow.AddHours(-24);

        // SE_PAY unpaid orders are handled by SePayExpiryJob (~30 min). This worker covers COD/other PENDING > 24h.
        var staleOrders = await context.Orders
            .Include(o => o.OrderDetails)
            .Where(o => o.PaymentStatus == "PENDING"
                     && o.PaymentMethod != "SE_PAY"
                     && !o.IsDeleted
                     && o.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (!staleOrders.Any())
        {
            _logger.LogInformation("No stale orders found");
            return;
        }

        foreach (var order in staleOrders)
        {

            order.PaymentStatus = "EXPIRED";

            var result = await lifecycleService.CancelOrderInternalAsync(
                order,
                "Payment timeout — auto cancelled after 24 hours",
                cancelledByAccountId: 0,   // System/Auto
                restoreCart: false,         // Auto-cancel does not restore cart
                cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Auto-cancelled Order {OrderCode} (payment timeout)", order.OrderCode);
            }
            else
            {
                _logger.LogWarning("Failed to auto-cancel order {OrderId}: {Error}", order.OrderId, result.ErrorMessage);
            }
        }

        _logger.LogInformation(
            "Auto-cancelled {Count} stale orders",
            staleOrders.Count);
    }
}
