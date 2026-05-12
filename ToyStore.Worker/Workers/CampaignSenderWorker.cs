using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Background worker that sends due admin campaigns using ICampaignNotificationService.
/// Runs every minute.
/// </summary>
public class CampaignSenderWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CampaignSenderWorker> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public CampaignSenderWorker(
        IServiceProvider serviceProvider, 
        ILogger<CampaignSenderWorker> logger,
        ITimeProvider timeProvider)
    {
        _serviceProvider = serviceProvider;
        _logger          = logger;
        _timeProvider    = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CampaignSenderWorker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueCampaignsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CampaignSenderWorker");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("CampaignSenderWorker stopping");
    }

    private async Task ProcessDueCampaignsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var context         = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignNotificationService>();

        var now = _timeProvider.UtcNow;

        var dueCampaignIds = await context.Campaigns
            .AsNoTracking()
            .Where(c => !c.IsDeleted
                     && c.Status == "Scheduled"
                     && c.ScheduledAt.HasValue
                     && c.ScheduledAt.Value <= now)
            .Select(c => c.CampaignId)
            .ToListAsync(cancellationToken);

        if (dueCampaignIds.Count == 0)
        {
            _logger.LogDebug("No due campaigns found");
            return;
        }

        _logger.LogInformation("Found {Count} due campaign(s) to send", dueCampaignIds.Count);

        foreach (var campaignId in dueCampaignIds)
        {
            try
            {
                await campaignService.DispatchAdminCampaignAsync(campaignId, cancellationToken);
                _logger.LogInformation("Campaign {CampaignId} dispatched successfully", campaignId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching campaign {CampaignId}", campaignId);
            }
        }
    }
}
