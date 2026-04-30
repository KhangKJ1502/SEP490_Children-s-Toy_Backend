using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Services.Resolvers;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Background worker gui thong bao cho cac Campaign da den gio gui (Status=Scheduled, ScheduledAt le now).
/// Chay moi 1 phut.
/// </summary>
public class CampaignSenderWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CampaignSenderWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public CampaignSenderWorker(IServiceProvider serviceProvider, ILogger<CampaignSenderWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger          = logger;
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
        var resolverFactory = scope.ServiceProvider.GetRequiredService<BusinessObjectResolverFactory>();
        var renderer        = scope.ServiceProvider.GetRequiredService<ITemplateRenderer>();

        var now = DateTime.UtcNow;

        // Load due campaigns with their templates and targets
        var dueCampaigns = await context.Campaigns
            .Include(c => c.CampaignTargets)
            .Include(c => c.TemplateCodeNavigation)
            .Where(c => !c.IsDeleted
                     && c.Status == "Scheduled"
                     && c.ScheduledAt.HasValue
                     && c.ScheduledAt.Value <= now)
            .ToListAsync(cancellationToken);

        if (dueCampaigns.Count == 0)
        {
            _logger.LogDebug("No due campaigns found");
            return;
        }

        _logger.LogInformation("Found {Count} due campaign(s) to send", dueCampaigns.Count);

        foreach (var campaign in dueCampaigns)
        {
            await SendCampaignAsync(campaign, context, resolverFactory, renderer, cancellationToken);
        }
    }

    private async Task SendCampaignAsync(
        Campaign campaign,
        SEP490ToyStoreContext context,
        BusinessObjectResolverFactory resolverFactory,
        ITemplateRenderer renderer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing campaign {CampaignId} '{CampaignName}'",
            campaign.CampaignId, campaign.CampaignName);

        // Mark as Sending immediately to prevent double-processing
        campaign.Status    = "Sending";
        campaign.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            // Resolve placeholders from the linked business object (if any)
            IReadOnlyDictionary<string, string> placeholders = new Dictionary<string, string>();
            string? resolvedActionTarget = campaign.ActionTarget;

            if (!string.IsNullOrWhiteSpace(campaign.ReferenceType) && campaign.ReferenceId.HasValue)
            {
                var resolved = await resolverFactory.ResolveAsync(
                    campaign.ReferenceType, campaign.ReferenceId.Value, cancellationToken);

                if (resolved is not null)
                {
                    placeholders = resolved.Placeholders;
                    resolvedActionTarget ??= resolved.DefaultActionTarget;
                }
            }

            // Render title and message — TitleOverride / MessageOverride take precedence over template
            var template = campaign.TemplateCodeNavigation;
            var rawTitle   = !string.IsNullOrWhiteSpace(campaign.TitleOverride)
                ? campaign.TitleOverride
                : template?.TitleTemplate ?? campaign.CampaignName;

            var rawMessage = !string.IsNullOrWhiteSpace(campaign.MessageOverride)
                ? campaign.MessageOverride
                : template?.MessageTemplate ?? string.Empty;

            var renderedTitle   = renderer.Render(rawTitle,   placeholders);
            var renderedMessage = renderer.Render(rawMessage, placeholders);

            // Resolve recipient account IDs
            var recipientIds = await ResolveRecipientIdsAsync(campaign, context, cancellationToken);

            if (recipientIds.Count == 0)
            {
                _logger.LogWarning(
                    "Campaign {CampaignId}: no recipients found — marking as Sent with 0 sent",
                    campaign.CampaignId);
            }
            else
            {
                // Bulk-insert Delivery records
                var deliveries = recipientIds.Select(accountId => new Delivery
                {
                    AccountId        = accountId,
                    CampaignId       = campaign.CampaignId,
                    TemplateCode     = campaign.TemplateCode,
                    RecipientType    = "CUSTOMER",
                    NotificationType = "PROMOTION",
                    ImageUrl         = campaign.ImageUrl,
                    ActionType       = campaign.ActionType,
                    ActionTarget     = resolvedActionTarget,
                    Title            = renderedTitle,
                    Message          = renderedMessage,
                    Payload          = "{}",
                    Status           = "Unread",
                    CreatedAt        = DateTime.UtcNow
                }).ToList();

                await context.Deliveries.AddRangeAsync(deliveries, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Campaign {CampaignId}: sent {Count} notifications",
                    campaign.CampaignId, deliveries.Count);
            }

            // Mark Sent and upsert CampaignStat
            campaign.Status    = "Sent";
            campaign.UpdatedAt = DateTime.UtcNow;

            var stat = await context.CampaignStats
                .Where(s => s.CampaignId == campaign.CampaignId)
                .FirstOrDefaultAsync(cancellationToken);

            if (stat is null)
            {
                stat = new CampaignStat
                {
                    CampaignId   = campaign.CampaignId,
                    TotalSent    = recipientIds.Count,
                    TotalRead    = 0,
                    TotalClicked = 0,
                    ComputedAt   = DateTime.UtcNow
                };
                await context.CampaignStats.AddAsync(stat, cancellationToken);
            }
            else
            {
                stat.TotalSent += recipientIds.Count;
                stat.ComputedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending campaign {CampaignId}", campaign.CampaignId);

            // Revert to Scheduled so the next tick can retry
            campaign.Status    = "Scheduled";
            campaign.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Giai quyet danh sach AccountId nhan thong bao dua tren TargetType cua campaign.
    /// </summary>
    private static async Task<List<int>> ResolveRecipientIdsAsync(
        Campaign campaign,
        SEP490ToyStoreContext context,
        CancellationToken cancellationToken)
    {
        switch (campaign.TargetType)
        {
            case "ALL":
                return await context.Accounts
                    .AsNoTracking()
                    .Where(a => a.IsActive && !a.IsDeleted)
                    .Select(a => a.AccountId)
                    .ToListAsync(cancellationToken);

            case "ROLE":
            {
                var roleIds = campaign.CampaignTargets
                    .Where(t => t.TargetType == "ROLE_ID")
                    .Select(t => t.TargetValue)
                    .ToHashSet();

                if (roleIds.Count == 0) return [];

                return await context.Accounts
                    .AsNoTracking()
                    .Where(a => a.IsActive && !a.IsDeleted && roleIds.Contains(a.RoleId.ToString()))
                    .Select(a => a.AccountId)
                    .ToListAsync(cancellationToken);
            }

            case "INDIVIDUAL":
            case "SEGMENT":
            {
                var accountIdStrings = campaign.CampaignTargets
                    .Where(t => t.TargetType == "ACCOUNT_ID")
                    .Select(t => t.TargetValue)
                    .ToHashSet();

                if (accountIdStrings.Count == 0) return [];

                // Parse to int to do an efficient IN query
                var accountIds = accountIdStrings
                    .Select(v => int.TryParse(v, out var id) ? id : 0)
                    .Where(id => id > 0)
                    .ToList();

                return await context.Accounts
                    .AsNoTracking()
                    .Where(a => a.IsActive && !a.IsDeleted && accountIds.Contains(a.AccountId))
                    .Select(a => a.AccountId)
                    .ToListAsync(cancellationToken);
            }

            default:
                return [];
        }
    }
}
