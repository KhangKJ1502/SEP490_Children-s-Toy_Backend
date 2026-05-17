using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Picks up ADMIN campaigns with Status='Scheduled' and ScheduledAt in the past.
/// Runs every 1 minute.
/// </summary>
public class CampaignSchedulerJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CampaignSchedulerJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public CampaignSchedulerJob(
        IServiceProvider services, 
        ILogger<CampaignSchedulerJob> logger,
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
            catch (Exception ex) { _logger.LogError(ex, "CampaignSchedulerJob error"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope    = _services.CreateScope();
        var db             = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var campaignSvc    = scope.ServiceProvider.GetRequiredService<ICampaignNotificationService>();

        bool success = true;
        string? message = null;

        try
        {
            var now = _timeProvider.UtcNow;

            var due = await db.Campaigns
                .Where(c => c.Status     == "Scheduled"
                         && !c.IsDeleted
                         && c.CampaignSchedule != null
                         && c.CampaignSchedule.ScheduledAt <= now)
                .Select(c => c.CampaignId)
                .ToListAsync(ct);

            foreach (var campaignId in due)
            {
                _logger.LogInformation("Dispatching scheduled campaign {Id}", campaignId);
                await campaignSvc.DispatchAdminCampaignAsync(campaignId, ct);
            }

            message = $"Dispatched {due.Count} scheduled campaigns";
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "CampaignSchedulerJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(
            scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>(),
            "CampaignSchedulerJob", success, message, _logger, ct);
    }
}
