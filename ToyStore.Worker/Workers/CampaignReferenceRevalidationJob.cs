using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Periodically re-validates template + reference data for ADMIN campaigns in <c>Scheduled</c> / <c>Waiting</c>.
/// Cancels with audit when content or reference is no longer valid.
/// Runs every 15 minutes.
/// </summary>
public sealed class CampaignReferenceRevalidationJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CampaignReferenceRevalidationJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(15);

    public CampaignReferenceRevalidationJob(
        IServiceProvider services,
        ILogger<CampaignReferenceRevalidationJob> logger,
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
            catch (Exception ex) { _logger.LogError(ex, "CampaignReferenceRevalidationJob error"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope  = _services.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var uow          = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var lifecycle    = scope.ServiceProvider.GetRequiredService<ICampaignLifecycleRules>();

        var success = true;
        string? message = null;

        try
        {
            var now = _timeProvider.UtcNow;

            var ids = await db.Campaigns
                .AsNoTracking()
                .Where(c => !c.IsDeleted
                         && c.Status == "Scheduled"
                         && c.SourceType == "ADMIN"
                         && c.CampaignSchedule != null
                         && c.CampaignSchedule.ExecutionStatus == "Waiting")
                .Select(c => c.CampaignId)
                .ToListAsync(ct);

            var invalidated = 0;

            foreach (var campaignId in ids)
            {
                var campaign = await uow.Campaigns.GetByIdAsync(campaignId, ct);
                if (campaign is null) continue;
                if (campaign.CampaignSchedule is not { ExecutionStatus: "Waiting" }) continue;

                var check = await lifecycle.ValidateDispatchAsync(campaign, ct);
                if (check.IsSuccess) continue;

                // Bug fix: Use SystemActorId=0 sentinel instead of hardcoded AccountId=1.
                const int SystemActorId = 0;
                var actor = campaign.CreatedByAccountId
                         ?? campaign.SubmittedByAccountId
                         ?? SystemActorId;

                var note = check.ErrorMessage ?? check.ErrorCode ?? "Reference revalidation failed";
                await uow.Campaigns.SystemCancelWithAuditAsync(campaignId, note, actor, now, ct);
                invalidated++;
                _logger.LogWarning(
                    "Campaign {CampaignId} cancelled after revalidation: {Code} {Message}",
                    campaignId,
                    check.ErrorCode,
                    note);
            }

            message = $"Revalidated {ids.Count} campaign(s); cancelled {invalidated}";
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "CampaignReferenceRevalidationJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(
            db,
            nameof(CampaignReferenceRevalidationJob),
            success,
            message,
            _logger,
            ct);
    }
}
