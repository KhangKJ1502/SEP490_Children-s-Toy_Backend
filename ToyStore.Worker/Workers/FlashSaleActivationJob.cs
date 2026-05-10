using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Detects flash sale slots becoming active and notifies all opted-in customers.
/// Runs every 1 minute.
/// </summary>
public class FlashSaleActivationJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<FlashSaleActivationJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public FlashSaleActivationJob(IServiceProvider services, ILogger<FlashSaleActivationJob> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "FlashSaleActivationJob error"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope    = _services.CreateScope();
        var db             = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var dispatcher     = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
        var prefChecker    = scope.ServiceProvider.GetRequiredService<IUserPreferenceChecker>();

        var nowUtc    = DateTime.UtcNow;
        var oneMinAgo = nowUtc.AddMinutes(-1);

        // Time slots whose StartAt is within the last 1 minute (just became active)
        var activeSlots = await db.PromotionTimeSlots
            .Include(s => s.Promotion)
            .Where(s => s.Status  == "Active"
                     && s.StartAt >= oneMinAgo
                     && s.StartAt <= nowUtc)
            .ToListAsync(ct);

        if (activeSlots.Count == 0) return;

        var customerIds = await db.Accounts
            .Where(a => a.RoleId == 1 && a.IsActive && !a.IsDeleted)
            .Select(a => a.AccountId)
            .ToListAsync(ct);

        foreach (var slot in activeSlots)
        {
            var eventKey = $"flash_sale:{slot.PromotionId}:{slot.TimeSlotId}";

            foreach (var accountId in customerIds)
            {
                if (!await prefChecker.CanSendAsync(accountId, PreferenceKeys.Promotions, ct))
                    continue;

                await dispatcher.DispatchAsync(new NotificationContext
                {
                    RecipientAccountId = accountId,
                    RecipientType      = RecipientTypes.Customer,
                    NotificationType   = NotificationTypes.Promotion,
                    Title              = "Flash Sale has started!",
                    Message            = $"{slot.Promotion.PromotionName} is happening right now!",
                    SendBell           = true,
                    SendEmail          = false,
                    TemplateCode       = NotificationTemplates.FlashSaleStarted,
                    IdempotencyKey     = $"{eventKey}:{accountId}:WEB_BELL",
                }, ct);
            }

            _logger.LogInformation("Flash sale notifications dispatched for slot {TimeSlotId}", slot.TimeSlotId);
        }
    }
}
