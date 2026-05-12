using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Quét các đơn SE_PAY PENDING quá TTL và tự động hủy (restore stock + voucher).
/// Chạy mỗi 5 phút.
/// </summary>
public class SePayExpiryJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SePayExpiryJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public SePayExpiryJob(
        IServiceProvider services, 
        ILogger<SePayExpiryJob> logger,
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
            catch (Exception ex) { _logger.LogError(ex, "SePayExpiryJob error"); }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope    = _services.CreateScope();
        var db             = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var sePayOpts      = scope.ServiceProvider.GetRequiredService<IOptions<SePayOptions>>().Value;
        var orderService   = scope.ServiceProvider.GetRequiredService<IOrderCustomerService>();

        var ttl    = TimeSpan.FromMinutes(sePayOpts.PaymentTtlMinutes);
        var cutoff = _timeProvider.UtcNow - ttl;

        // Lấy đơn SE_PAY PENDING quá TTL
        var expiredOrders = await db.Orders
            .Where(o => o.PaymentMethod == "SE_PAY"
                     && o.PaymentStatus == "PENDING"
                     && !o.IsDeleted
                     && o.CancelledAt == null
                     && o.CreatedAt < cutoff)
            .Select(o => o.OrderId)
            .ToListAsync(ct);

        _logger.LogInformation("SePayExpiryJob: found {Count} expired SE_PAY orders", expiredOrders.Count);

        foreach (var orderId in expiredOrders)
        {
            try
            {
                // Kiểm tra idempotency — re-load để tránh race condition
                var order = await db.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.PaymentStatus == "PENDING", ct);
                if (order is null) continue;

                var result = await orderService.CancelAsync(
                    orderId,
                    actorAccountId: null, // system
                    isAdmin: true,
                    reason: "SE_PAY payment timeout — auto-cancelled by system",
                    ct);

                if (result.IsSuccess)
                    _logger.LogInformation("SePayExpiryJob: auto-cancelled order {Id}", orderId);
                else
                    _logger.LogWarning("SePayExpiryJob: failed to cancel order {Id}: {Err}", orderId, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SePayExpiryJob: error processing order {Id}", orderId);
            }
        }

        // Update PaymentStatus = EXPIRED cho các attempt Pending của đơn đã hủy
        await db.PaymentGatewayTransactions
            .Where(t => t.Status == "Pending"
                     && t.Order.PaymentMethod == "SE_PAY"
                     && t.Order.CancelledAt != null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, "Cancelled"), ct);
    }
}
