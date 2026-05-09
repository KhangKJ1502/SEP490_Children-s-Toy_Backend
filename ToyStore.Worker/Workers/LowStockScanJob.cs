using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Scans products at or below StockThreshold and notifies all Merchandise staff.
/// Respects LowStockNotificationEnabled and 24-hour cool-down per product.
/// Runs every 1 hour.
/// </summary>
public class LowStockScanJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LowStockScanJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public LowStockScanJob(IServiceProvider services, ILogger<LowStockScanJob> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "LowStockScanJob error"); }

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
            var now = DateTime.UtcNow;
            var cooldown = now.AddHours(-24);

            var lowStockProducts = await db.Products
                .Where(p => !p.IsDeleted
                         && p.ProductStatus == "Active"
                         && p.LowStockNotificationEnabled
                         && p.Quantity > 0
                         && p.Quantity <= p.StockThreshold
                         && (p.LastLowStockNotifiedAt == null || p.LastLowStockNotifiedAt < cooldown))
                .ToListAsync(ct);

            var outOfStockProducts = await db.Products
                .Where(p => !p.IsDeleted && p.Quantity == 0 && p.ProductStatus == "OutOfStock")
                .ToListAsync(ct);

            // Merchandise role accounts (assumed RoleId = 3 — adjust to actual)
            var merchAccounts = await db.Accounts
                .Where(a => a.Role.RoleName == "Merchandise" && a.IsActive && !a.IsDeleted)
                .Select(a => new { a.AccountId, a.Email, a.AccountName })
                .ToListAsync(ct);

            foreach (var product in lowStockProducts)
            {
                foreach (var merch in merchAccounts)
                {
                    await dispatcher.DispatchAsync(new NotificationContext
                    {
                        RecipientAccountId = merch.AccountId,
                        RecipientType      = RecipientTypes.Merchandise,
                        NotificationType   = NotificationTypes.Stock,
                        Title              = "Sắp hết hàng",
                        Message            = $"Sản phẩm '{product.ProductName}' chỉ còn {product.Quantity} cái (ngưỡng: {product.StockThreshold})",
                        SendBell           = true,
                        SendEmail          = false,
                        TemplateCode       = NotificationTemplates.MerchLowStock,
                        IdempotencyKey     = $"product.low_stock:{product.ProductId}:{now:yyyyMMddHH}:{merch.AccountId}",
                    }, ct);
                }

                product.LastLowStockNotifiedAt = now;
                product.UpdatedAt              = now;
            }

            foreach (var product in outOfStockProducts)
            {
                foreach (var merch in merchAccounts)
                {
                    await dispatcher.DispatchAsync(new NotificationContext
                    {
                        RecipientAccountId = merch.AccountId,
                        RecipientType      = RecipientTypes.Merchandise,
                        NotificationType   = NotificationTypes.Stock,
                        Title              = "Hết hàng",
                        Message            = $"Sản phẩm '{product.ProductName}' đã hết hàng",
                        SendBell           = true,
                        SendEmail          = true,
                        TemplateCode       = NotificationTemplates.MerchOutOfStock,
                        IdempotencyKey     = $"product.out_of_stock:{product.ProductId}:{now:yyyyMMdd}:{merch.AccountId}",
                    }, ct);
                }
            }

            await db.SaveChangesAsync(ct);
            message = $"LowStock={lowStockProducts.Count} OutOfStock={outOfStockProducts.Count}";
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "LowStockScanJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(db, "LowStockScanJob", success, message, _logger, ct);
    }
}
