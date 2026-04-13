using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Enums;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Background worker that processes pending order status updates.
/// </summary>
public class OrderStatusWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderStatusWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    
    public OrderStatusWorker(IServiceProvider serviceProvider, ILogger<OrderStatusWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        _logger.LogInformation("Checking for pending orders to process");
        
        var pendingOrders = await unitOfWork.Orders.GetPendingOrdersAsync(cancellationToken);
        
        foreach (var order in pendingOrders)
        {
            try
            {
                // Process order based on payment status
                if (order.PaymentStatus == "Paid" && order.Status == Domain.Enums.OrderStatus.Pending)
                {
                    order.Status = Domain.Enums.OrderStatus.Confirmed;
                    order.UpdatedAt = DateTime.UtcNow;
                    unitOfWork.Orders.Update(order);
                    
                    _logger.LogInformation(
                        "Order {OrderNumber} status updated to Confirmed",
                        order.OrderNumber);
                }
                
                // Check for stale pending orders (older than 24 hours without payment)
                if (order.Status == Domain.Enums.OrderStatus.Pending && 
                    order.PaymentStatus == "Pending" &&
                    order.CreatedAt < DateTime.UtcNow.AddHours(-24))
                {
                    order.Status = Domain.Enums.OrderStatus.Cancelled;
                    order.CancelledAt = DateTime.UtcNow;
                    order.CancellationReason = "Payment timeout - auto cancelled after 24 hours";
                    order.UpdatedAt = DateTime.UtcNow;
                    unitOfWork.Orders.Update(order);
                    
                    _logger.LogInformation(
                        "Order {OrderNumber} auto-cancelled due to payment timeout",
                        order.OrderNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order {OrderNumber}", order.OrderNumber);
            }
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Processed {Count} pending orders", pendingOrders.Count);
    }
}
