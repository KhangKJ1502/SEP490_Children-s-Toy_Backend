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
        _logger          = logger;
        _timeProvider    = timeProvider;
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

        _logger.LogInformation("Checking for pending orders to auto-cancel");

        // Huỷ đơn PENDING chưa thanh toán sau 24h
        var cutoff = _timeProvider.UtcNow.AddHours(-24);

        var staleOrders = await context.Orders
            .Where(o => o.PaymentStatus == "PENDING"
                     && !o.IsDeleted
                     && o.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (!staleOrders.Any())
        {
            _logger.LogInformation("No stale orders found");
            return;
        }

        // Lấy StatusID cho "Cancelled"
        var cancelledStatus = await context.StatusOrders
            .FirstOrDefaultAsync(s => s.StatusName == "Cancelled", cancellationToken);

        if (cancelledStatus == null)
        {
            _logger.LogWarning("StatusOrder 'Cancelled' not found in DB — skipping auto-cancel");
            return;
        }

        foreach (var order in staleOrders)
        {
            order.PaymentStatus = "FAILED";
            order.StatusId = cancelledStatus.StatusId;
            order.CancelledAt = _timeProvider.UtcNow;
            order.CancelReason = "Payment timeout — auto cancelled after 24 hours";
            order.UpdatedAt = _timeProvider.UtcNow;

            _logger.LogInformation(
                "Auto-cancelled Order {OrderCode} (payment timeout)",
                order.OrderCode);
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Auto-cancelled {Count} stale orders",
            staleOrders.Count);
    }
}
