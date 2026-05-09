using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Services.Notifications;

public class CampaignNotificationService : ICampaignNotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IUserPreferenceChecker _prefChecker;
    private readonly ILogger<CampaignNotificationService> _logger;

    public CampaignNotificationService(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher,
        IUserPreferenceChecker prefChecker,
        ILogger<CampaignNotificationService> logger)
    {
        _unitOfWork  = unitOfWork;
        _dispatcher  = dispatcher;
        _prefChecker = prefChecker;
        _logger      = logger;
    }

    public async Task ProcessSystemCampaignAsync(
        string eventKey,
        Dictionary<string, string> vars,
        CancellationToken ct = default)
    {
        var campaign = await _unitOfWork.Campaigns.GetByEventKeyAsync(eventKey, ct);

        if (campaign is null)
        {
            _logger.LogWarning("No SYSTEM campaign found for EventKey={EventKey}", eventKey);
            return;
        }

        if (campaign.Status == "Sent")
        {
            _logger.LogInformation("SYSTEM campaign already sent. CampaignID={Id}", campaign.CampaignId);
            return;
        }

        await DispatchCampaignAsync(campaign, vars, ct);
    }

    public async Task DispatchAdminCampaignAsync(int campaignId, CancellationToken ct = default)
    {
        var campaign = await _unitOfWork.Campaigns.GetByIdWithDetailsAsync(campaignId, ct);

        if (campaign is null)
        {
            _logger.LogWarning("Campaign not found. CampaignID={Id}", campaignId);
            return;
        }

        if (campaign.Status is "Sent" or "Cancelled")
        {
            _logger.LogWarning("Campaign {Id} is already {Status} — skipping", campaignId, campaign.Status);
            return;
        }

        campaign.Status    = "Sending";
        campaign.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        await DispatchCampaignAsync(campaign, new Dictionary<string, string>(), ct);
    }

    private async Task DispatchCampaignAsync(
        Campaign campaign,
        Dictionary<string, string> vars,
        CancellationToken ct)
    {
        var (title, message) = ResolveContent(campaign, vars);

        var accountIds = await ResolveTargetAccountsAsync(campaign, ct);
        var notifType  = ResolveNotificationType(campaign.TemplateCode);

        int sent = 0;
        foreach (var accountId in accountIds)
        {
            if (notifType == NotificationTypes.Promotion)
            {
                if (!await _prefChecker.CanSendAsync(accountId, PreferenceKeys.Promotions, ct))
                    continue;
            }

            var idempotencyBase = $"campaign:{campaign.CampaignId}:{accountId}";

            var ctx = new NotificationContext
            {
                RecipientAccountId = accountId,
                RecipientType      = RecipientTypes.Customer,
                NotificationType   = notifType,
                Title              = title,
                Message            = message,
                SendBell           = true,
                SendEmail          = campaign.SourceType == "ADMIN",
                TemplateCode       = campaign.TemplateCode,
                ImageUrl           = campaign.ImageUrl,
                ActionType         = campaign.ActionType,
                ActionTarget       = campaign.ActionTarget,
                CampaignId         = campaign.CampaignId,
                IdempotencyKey     = idempotencyBase,
            };

            await _dispatcher.DispatchAsync(ctx, ct);
            sent++;
        }

        campaign.Status    = "Sent";
        campaign.UpdatedAt = DateTime.UtcNow;

        var stat = campaign.CampaignStat;
        if (stat is null)
        {
            stat = new CampaignStat { CampaignId = campaign.CampaignId };
            // Note: We need a way to add stats via unit of work if it's not tracked
            // For now assuming the campaign include handled it
        }

        stat.TotalSent  += sent;
        stat.ComputedAt  = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Campaign {Id} dispatched. Sent={Sent}",
            campaign.CampaignId, sent);
    }

    private (string title, string message) ResolveContent(Campaign campaign, Dictionary<string, string> vars)
    {
        var title   = campaign.TitleOverride
                   ?? InterpolateVars(campaign.TemplateCodeNavigation?.TitleTemplate, vars)
                   ?? campaign.CampaignName;

        var message = campaign.MessageOverride
                   ?? InterpolateVars(campaign.TemplateCodeNavigation?.MessageTemplate, vars)
                   ?? string.Empty;

        return (title, message);
    }

    private static string? InterpolateVars(string? template, Dictionary<string, string> vars)
    {
        if (template is null) return null;
        foreach (var (key, value) in vars)
            template = template.Replace($"{{{key}}}", value);
        return template;
    }

    private async Task<List<int>> ResolveTargetAccountsAsync(Campaign campaign, CancellationToken ct)
    {
        return campaign.TargetType switch
        {
            "ALL"        => await ResolveAllCustomersAsync(ct),
            "ROLE"       => await ResolveByRoleAsync(campaign.CampaignTargets, ct),
            "INDIVIDUAL" => ResolveIndividual(campaign.CampaignTargets),
            _            => new List<int>(),
        };
    }

    private async Task<List<int>> ResolveAllCustomersAsync(CancellationToken ct)
    {
        var customers = await _unitOfWork.Accounts.GetActiveCustomersAsync(ct);
        return customers.Select(a => a.AccountId).ToList();
    }

    private async Task<List<int>> ResolveByRoleAsync(
        ICollection<CampaignTarget> targets, CancellationToken ct)
    {
        var roleIds = targets
            .Where(t => t.TargetType == "ROLE_ID")
            .Select(t => byte.TryParse(t.TargetValue, out var r) ? r : (byte)0)
            .Where(r => r > 0)
            .ToArray();

        var accounts = await _unitOfWork.Accounts.GetByRoleIdsAsync(roleIds, ct);
        return accounts.Select(a => a.AccountId).ToList();
    }

    private static List<int> ResolveIndividual(ICollection<CampaignTarget> targets)
        => targets
            .Where(t => t.TargetType == "ACCOUNT_ID")
            .Select(t => int.TryParse(t.TargetValue, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();

    private static string ResolveNotificationType(string? templateCode)
    {
        if (templateCode is null) return NotificationTypes.Promotion;

        return templateCode switch
        {
            var t when t.StartsWith("ORDER")  => NotificationTypes.Order,
            var t when t.StartsWith("WALLET") => NotificationTypes.Order,
            var t when t.StartsWith("MERCH")  => NotificationTypes.Stock,
            var t when t.StartsWith("ADMIN")  => NotificationTypes.System,
            var t when t.StartsWith("BLOG")   => NotificationTypes.Blog,
            _                                  => NotificationTypes.Promotion,
        };
    }
}
