using Microsoft.EntityFrameworkCore;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Handles state transition of both Promotions and their associated Time Slots.
/// Runs every 1 minute.
/// </summary>
public class PromotionStatusJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PromotionStatusJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public PromotionStatusJob(IServiceProvider services, ILogger<PromotionStatusJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "PromotionStatusJob error"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        
        bool success = true;
        string? message = null;
        
        try
        {
            var nowUtc = DateTime.UtcNow;

            // 1. Scheduled -> Active when StartDate <= now
            var scheduledPromotions = await db.Promotions
                .Where(p => p.Status == "Scheduled" && !p.IsDeleted && p.StartDate <= nowUtc)
                .ToListAsync(ct);
            foreach (var p in scheduledPromotions)
            {
                p.Status = "Active";
                p.UpdatedAt = nowUtc;
            }

            // 2. Active -> Expired when EndDate < now
            var activePromotions = await db.Promotions
                .Where(p => p.Status == "Active" && !p.IsDeleted && p.EndDate < nowUtc)
                .ToListAsync(ct);
            foreach (var p in activePromotions)
            {
                p.Status = "Expired";
                p.UpdatedAt = nowUtc;
            }

            // 3. TimeSlot Scheduled -> Active when StartAt <= now
            var scheduledSlots = await db.PromotionTimeSlots
                .Where(s => s.Status == "Scheduled" && s.StartAt <= nowUtc)
                .ToListAsync(ct);
            foreach (var s in scheduledSlots)
            {
                s.Status = "Active";
                s.UpdatedAt = nowUtc;
            }

            // 4. TimeSlot Active -> Expired when EndAt < now
            var activeSlots = await db.PromotionTimeSlots
                .Where(s => s.Status == "Active" && s.EndAt < nowUtc)
                .ToListAsync(ct);
            foreach (var s in activeSlots)
            {
                s.Status = "Expired";
                s.UpdatedAt = nowUtc;
            }
            
            int totalUpdated = scheduledPromotions.Count + activePromotions.Count + scheduledSlots.Count + activeSlots.Count;
            if (totalUpdated > 0)
            {
                await db.SaveChangesAsync(ct);
                message = $"Updated {scheduledPromotions.Count} scheduled promos, {activePromotions.Count} active promos, {scheduledSlots.Count} scheduled slots, {activeSlots.Count} active slots.";
                _logger.LogInformation("PromotionStatusJob: {Message}", message);
            }
            else
            {
                message = "No status updates required.";
            }
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "PromotionStatusJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(
            scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>(),
            "PromotionStatusJob", success, message, _logger, ct);
    }
}
