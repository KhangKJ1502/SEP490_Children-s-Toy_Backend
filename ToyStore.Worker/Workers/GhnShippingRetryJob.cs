using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Common.Helpers;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Retry tạo đơn GHN cho các ShippingProviderTransactions có RetryCount > 0 và chưa có ProviderOrderCode.
/// Cũng mirror GHN status sang Orders.StatusID theo GhnStatusMap (§9.13).
/// Chạy mỗi 10 phút.
/// </summary>
public class GhnShippingRetryJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<GhnShippingRetryJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);
    private const int MaxRetries = 5;

    public GhnShippingRetryJob(
        IServiceProvider services, 
        ILogger<GhnShippingRetryJob> logger,
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
            try { await RunRetryAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "GhnShippingRetryJob error"); }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunRetryAsync(CancellationToken ct)
    {
        using var scope   = _services.CreateScope();
        var db            = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var ghnClient     = scope.ServiceProvider.GetRequiredService<IGhnClient>();
        var ghnOpts       = scope.ServiceProvider.GetRequiredService<IOptions<GhnOptions>>().Value;
        var sePayOpts     = scope.ServiceProvider.GetRequiredService<IOptions<SePayOptions>>().Value;

        var orderRepo     = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        // Tìm các đơn cần retry: có ShippingProviderTransaction chưa có ProviderOrderCode + RetryCount > 0
        var pendingShipments = await db.ShippingProviderTransactions
            .Include(t => t.Order)
                .ThenInclude(o => o.OrderDetails)
            .Where(t => string.IsNullOrEmpty(t.ProviderOrderCode)
                     && t.RetryCount > 0
                     && t.RetryCount <= MaxRetries
                     && (t.Order.PaymentStatus == "PAID" || t.Order.PaymentStatus == "COD_PENDING")
                     && t.Order.CancelledAt == null)
            .ToListAsync(ct);

        _logger.LogInformation("GhnShippingRetryJob: {Count} shipments to retry", pendingShipments.Count);

        foreach (var txn in pendingShipments)
        {
            try
            {
                var order = txn.Order;

                if (txn.RetryCount > MaxRetries)
                {
                    _logger.LogWarning("GHN retry exceeded max for Order {Code}", order.OrderCode);
                    continue;
                }

                var shippingItems = await orderRepo.GetShippingItemsForOrderAsync(order.OrderId, ct);
                var package = GhnPackageCalculator.CalculateForServiceType(
                    shippingItems,
                    GhnShippingLimits.Type2ServiceId,
                    ghnOpts.DefaultItemWeight,
                    ghnOpts.DefaultLength,
                    ghnOpts.DefaultWidth,
                    ghnOpts.DefaultHeight);

                var codAmount = order.PaymentMethod == "SHIP_COD" ? order.TotalAmount : 0m;
                var ghnRequest = new ShippingOrderCreateRequestDto
                {
                    ToName          = order.ShippingName,
                    ToPhone         = order.ShippingPhone,
                    ToAddress       = order.ShippingAddress,
                    ToWardCode      = order.ShippingWardCode,
                    ToDistrictId    = order.ShippingDistrictId,
                    CodAmount       = codAmount,
                    Weight          = Math.Max(package.Weight, 1),
                    Length          = package.Length,
                    Width           = package.Width,
                    Height          = package.Height,
                    InsuranceValue  = order.SubTotal,
                    ClientOrderCode = order.OrderCode,
                    ServiceTypeId   = package.ServiceTypeId,
                    Items = package.Items.Select(x => new ShippingOrderCreateItemDto
                    {
                        Name     = x.Name,
                        Code     = x.Code,
                        Quantity = x.Quantity,
                        Price    = x.Price,
                        Weight   = x.Weight,
                        Length   = x.Length,
                        Width    = x.Width,
                        Height   = x.Height,
                        Category = x.Category
                    }).ToList()
                };

                var result = await ghnClient.CreateOrderAsync(ghnRequest, ct);
                if (result.IsSuccess)
                {
                    decimal actualFee         = result.Data!.TotalFee;
                    order.ShippingOrderCode   = result.Data.OrderCode;
                    txn.ProviderOrderCode     = result.Data.OrderCode;
                    txn.TrackingNumber        = result.Data.OrderCode;
                    txn.Status                = "ready_to_pick";
                    txn.EstimatedDelivery     = result.Data.ExpectedDeliveryTime;
                    txn.LastErrorMessage      = null;
                    txn.UpdatedAt             = _timeProvider.UtcNow;
                    if (actualFee > 0)
                    {
                        txn.ShippingFee            = actualFee;
                        order.ActualShippingFee    = actualFee;
                        order.EstimatedShippingFee = actualFee;
                    }
                    _logger.LogInformation("GHN retry success for Order {Code}: {GhnCode}",
                        order.OrderCode, result.Data.OrderCode);
                }
                else
                {
                    txn.RetryCount++;
                    txn.LastErrorMessage = result.ErrorMessage;
                    txn.UpdatedAt        = _timeProvider.UtcNow;
                    _logger.LogWarning("GHN retry failed for Order {Code}: {Err} (attempt {N})",
                        order.OrderCode, result.ErrorMessage, txn.RetryCount);
                }

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GHN retry error for txn {Id}", txn.ShippingTransactionId);
            }
        }
    }
}
