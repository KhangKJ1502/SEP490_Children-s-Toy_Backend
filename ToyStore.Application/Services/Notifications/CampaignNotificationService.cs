using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Common.Helpers;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Services.Notifications;

public class CampaignNotificationService : ICampaignNotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IUserPreferenceChecker _prefChecker;
    private readonly ILogger<CampaignNotificationService> _logger;
    private readonly ICampaignLifecycleRules _lifecycleRules;
    private readonly ITimeProvider _timeProvider;

    public CampaignNotificationService(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher,
        IUserPreferenceChecker prefChecker,
        ILogger<CampaignNotificationService> logger,
        ICampaignLifecycleRules lifecycleRules,
        ITimeProvider timeProvider)
    {
        _unitOfWork       = unitOfWork;
        _dispatcher       = dispatcher;
        _prefChecker      = prefChecker;
        _logger           = logger;
        _lifecycleRules   = lifecycleRules;
        _timeProvider     = timeProvider;
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
        var now = _timeProvider.UtcNow;
        var preview = await _unitOfWork.Campaigns.GetByIdAsync(campaignId, ct);
        if (preview is null)
        {
            _logger.LogWarning("Campaign not found. CampaignID={Id}", campaignId);
            return;
        }

        if (_lifecycleRules.ShouldSkipDispatch(preview, now, out var skipReason))
        {
            _logger.LogDebug("Skip campaign {Id}: {Reason}", campaignId, skipReason);
            return;
        }

        int jobId;
        try
        {
            jobId = await _unitOfWork.Campaigns.GetBackgroundJobIdForCampaignLockAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot resolve BackgroundJob id for campaign lock");
            return;
        }

        if (!await _unitOfWork.Campaigns.TryAcquireDispatchLockAsync(campaignId, jobId, now, ct))
            return;

        var campaign = await _unitOfWork.Campaigns.GetForUpdateAsync(campaignId, ct);
        if (campaign is null) return;

        var dVal = await _lifecycleRules.ValidateDispatchAsync(campaign, ct);
        if (dVal.IsFailure)
        {
            var actor = campaign.CreatedByAccountId
                     ?? campaign.SubmittedByAccountId
                     ?? (await _unitOfWork.Accounts.GetByIdAsync(1, ct))?.AccountId
                     ?? 1;

            await _unitOfWork.Campaigns.SystemCancelWithAuditAsync(
                campaignId, dVal.ErrorMessage ?? "Dispatch validation failed", actor, now, ct);
            return;
        }

        campaign.Status    = "Sending";
        campaign.UpdatedAt = now;
        await _unitOfWork.SaveChangesAsync(ct);

        try
        {
            var vars = await ResolveCampaignVariablesAsync(campaign, ct);
            var sent = await DispatchFanOutAsync(campaign, vars, ct);
            if (sent == 0)
            {
                _logger.LogWarning("Campaign {Id} dispatched with 0 bell recipients — marking Failed", campaignId);
                await _unitOfWork.Campaigns.HandleDispatchFailureAsync(campaignId, "No bell recipients received the notification.", now, ct);
            }
            else
            {
                await _unitOfWork.Campaigns.CompleteDispatchAsync(campaignId, sent, now, ct);
                _logger.LogInformation("Campaign {Id} dispatched. BellRecipients={Sent}", campaignId, sent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Campaign {Id} dispatch failed", campaignId);
            await _unitOfWork.Campaigns.HandleDispatchFailureAsync(campaignId, ex.Message, now, ct);
        }
    }

    private async Task<int> DispatchFanOutAsync(
        Campaign campaign,
        Dictionary<string, string> vars,
        CancellationToken ct)
    {
        var (title, message) = ResolveContent(campaign, vars);
        var accountIds = await ResolveTargetAccountsAsync(campaign, ct);
        var notifType  = ResolveNotificationType(campaign);

        var sent = 0;
        foreach (var accountId in accountIds)
        {
            if (!await PassesCampaignPreferencesAsync(campaign, accountId, notifType, ct))
                continue;

            var sendEmail = string.Equals(campaign.SourceType, "ADMIN", StringComparison.OrdinalIgnoreCase)
                && await _prefChecker.CanReceiveEmailAsync(accountId, ct);

            var idempotencyBase = $"campaign:{campaign.CampaignId}:{accountId}";
            var bellKey = $"{idempotencyBase}:{NotificationChannels.WebBell}";

            // Check before dispatch so we can detect new bell deliveries
            var bellExistedBefore = await _unitOfWork.Deliveries.ExistsByIdempotencyKeyAsync(bellKey, ct);

            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = accountId,
                RecipientType      = RecipientTypes.Customer,
                NotificationType   = notifType,
                Title              = title,
                Message            = message,
                SendBell           = true,
                SendEmail          = sendEmail,
                TemplateCode       = campaign.TemplateCode,
                ImageUrl           = campaign.ImageUrl,
                ActionType         = campaign.ActionType,
                ActionTarget       = campaign.ActionTarget,
                CampaignId         = campaign.CampaignId,
                IdempotencyKey     = idempotencyBase,
            }, ct);

            // Count only new WEB_BELL deliveries (dedupe per recipient)
            if (!bellExistedBefore && await _unitOfWork.Deliveries.ExistsByIdempotencyKeyAsync(bellKey, ct))
                sent++;
        }

        return sent;
    }

    private async Task<bool> PassesCampaignPreferencesAsync(
        Campaign campaign,
        int accountId,
        string notifType,
        CancellationToken ct)
    {
        if (string.Equals(campaign.ReferenceType, "VOUCHER", StringComparison.OrdinalIgnoreCase)
            || string.Equals(campaign.ReferenceType, "SALE", StringComparison.OrdinalIgnoreCase))
            return await _prefChecker.CanSendAsync(accountId, PreferenceKeys.Promotions, ct);

        if (string.Equals(campaign.ReferenceType, "BLOG", StringComparison.OrdinalIgnoreCase)
            || notifType == NotificationTypes.Blog)
            return await _prefChecker.CanSendAsync(accountId, PreferenceKeys.BlogAlerts, ct);

        if (notifType == NotificationTypes.Stock)
            return await _prefChecker.CanSendAsync(accountId, PreferenceKeys.StockAlerts, ct);

        if (notifType == NotificationTypes.Promotion)
            return await _prefChecker.CanSendAsync(accountId, PreferenceKeys.Promotions, ct);

        return true;
    }

    private async Task DispatchCampaignAsync(
        Campaign campaign,
        Dictionary<string, string> vars,
        CancellationToken ct)
    {
        var sent = await DispatchFanOutAsync(campaign, vars, ct);
        try
        {
            await _unitOfWork.Campaigns.MarkSentAsync(campaign.CampaignId, sent, ct);
            _logger.LogInformation("Campaign {Id} dispatched. Sent={Sent}", campaign.CampaignId, sent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Campaign {Id} dispatch save failed.", campaign.CampaignId);
        }
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
        {
            template = template.Replace("{{" + key + "}}", value);
            template = template.Replace("{" + key + "}", value);
        }
        return template;
    }

    private async Task<Dictionary<string, string>> ResolveCampaignVariablesAsync(Campaign campaign, CancellationToken ct)
    {
        var vars = new Dictionary<string, string>();

        if (!campaign.ReferenceId.HasValue || string.IsNullOrEmpty(campaign.ReferenceType))
            return vars;

        try
        {
            switch (campaign.ReferenceType)
            {
                case "VOUCHER":
                    var voucher = await _unitOfWork.Vouchers.GetByIdAsync(campaign.ReferenceId.Value, ct);
                    if (voucher != null)
                    {
                        vars["VoucherCode"] = voucher.VoucherCode;
                        vars["VoucherName"] = voucher.VoucherName;
                        vars["DiscountValue"] = voucher.DiscountType == "PERCENTAGE" 
                            ? $"{voucher.DiscountValue:0.##}%" 
                            : $"{voucher.DiscountValue:N0} VND";
                        vars["DiscountType"] = voucher.DiscountType == "PERCENTAGE" ? "percentage discount" : "fixed discount";
                        vars["MinOrderAmount"] = voucher.MinOrderAmount.HasValue ? $"{voucher.MinOrderAmount.Value:N0} VND" : "0 VND";
                        vars["MaxDiscountCap"] = voucher.MaxDiscountCap.HasValue ? $"{voucher.MaxDiscountCap.Value:N0} VND" : "No limit";
                        
                        var voucherUtc = voucher.EndDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(voucher.EndDate, DateTimeKind.Utc) : voucher.EndDate.ToUniversalTime();
                        vars["ExpiryDate"] = DateTimeHelper.FormatVietnamese(DateTimeHelper.ToVietnamTime(voucherUtc));
                    }
                    break;

                case "PRODUCT":
                    var product = await _unitOfWork.Products.GetByIdAsync(campaign.ReferenceId.Value, ct);
                    if (product != null)
                    {
                        vars["ProductName"] = product.ProductName;
                        vars["Price"] = $"{product.Price:N0} VND";
                    }
                    break;

                case "SALE":
                    var promo = await _unitOfWork.Promotions.GetByIdAsync(campaign.ReferenceId.Value, ct);
                    if (promo != null)
                    {
                        vars["PromotionName"] = promo.PromotionName;

                        var startUtc = promo.StartDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(promo.StartDate, DateTimeKind.Utc) : promo.StartDate.ToUniversalTime();
                        vars["StartDate"] = DateTimeHelper.FormatVietnamese(DateTimeHelper.ToVietnamTime(startUtc));

                        var endUtc = promo.EndDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(promo.EndDate, DateTimeKind.Utc) : promo.EndDate.ToUniversalTime();
                        vars["EndDate"] = DateTimeHelper.FormatVietnamese(DateTimeHelper.ToVietnamTime(endUtc));
                    }
                    break;
                    
                case "BLOG":
                    var blog = await _unitOfWork.Blogs.GetByIdAsync(campaign.ReferenceId.Value, ct);
                    if (blog != null)
                    {
                        vars["BlogTitle"] = blog.BlogTitle;
                        vars["CategoryName"] = blog.BlogCategory?.BlogCategoriesName ?? "General";
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve campaign variables for ReferenceType={Type}, ReferenceId={Id}", campaign.ReferenceType, campaign.ReferenceId);
        }

        return vars;
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
        return customers.Select(a => a.AccountId).Distinct().ToList();
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
        return accounts.Select(a => a.AccountId).Distinct().ToList();
    }

    private static List<int> ResolveIndividual(ICollection<CampaignTarget> targets)
        => targets
            .Where(t => t.TargetType == "ACCOUNT_ID")
            .Select(t => int.TryParse(t.TargetValue, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

    private static string ResolveNotificationType(Campaign campaign)
    {
        if (!string.IsNullOrWhiteSpace(campaign.ReferenceType))
        {
            var refType = campaign.ReferenceType.Trim().ToUpperInvariant();
            switch (refType)
            {
                case "BLOG":
                    return NotificationTypes.Blog;
                case "PRODUCT":
                    return NotificationTypes.Stock;
                case "VOUCHER":
                case "SALE":
                    return NotificationTypes.Promotion;
            }
        }

        if (campaign.TemplateCode is null) return NotificationTypes.Promotion;

        return campaign.TemplateCode switch
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
