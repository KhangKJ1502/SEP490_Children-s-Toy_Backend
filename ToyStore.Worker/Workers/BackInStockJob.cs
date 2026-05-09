using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Notifies customers who follow a product when it becomes available again.
/// Runs every 30 minutes.
/// </summary>
public class BackInStockJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BackInStockJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);

    public BackInStockJob(IServiceProvider services, ILogger<BackInStockJob> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "BackInStockJob error"); }

            await Task.Delay(_interval, stoppingToken);
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
            // Followers who have not yet been notified, whose product is now in stock
            var pendingFollowers = await db.ProductFollowers
                .Include(f => f.Product)
                .Where(f => f.NotifiedAt == null
                         && f.Product.Quantity > 0
                         && f.Product.ProductStatus == "Active"
                         && !f.Product.IsDeleted)
                .ToListAsync(ct);

            int count = 0;
            foreach (var follower in pendingFollowers)
            {
                if (!await prefChecker.CanSendAsync(follower.AccountId, PreferenceKeys.StockAlerts, ct))
                    continue;

                await dispatcher.DispatchAsync(new NotificationContext
                {
                    RecipientAccountId = follower.AccountId,
                    RecipientType      = RecipientTypes.Customer,
                    NotificationType   = NotificationTypes.Stock,
                    Title              = "Sản phẩm có hàng trở lại",
                    Message            = $"'{follower.Product.ProductName}' đã có hàng. Mua ngay kẻo hết!",
                    SendBell           = true,
                    SendEmail          = true,
                    TemplateCode       = NotificationTemplates.ProductBackInStock,
                    ActionTarget       = $"/products/{follower.ProductId}",
                    IdempotencyKey     = $"product.back_in_stock:{follower.ProductId}:{follower.AccountId}",
                }, ct);

                follower.NotifiedAt = DateTime.UtcNow;
                count++;
            }

            await db.SaveChangesAsync(ct);
            message = $"Back-in-stock notifications sent: {count}";
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "BackInStockJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(db, "BackInStockJob", success, message, _logger, ct);
    }
}
