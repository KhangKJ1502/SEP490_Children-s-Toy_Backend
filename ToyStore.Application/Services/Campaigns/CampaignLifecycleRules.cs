using ToyStore.Application.Campaigns;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Services.Campaigns;

public sealed class CampaignLifecycleRules : ICampaignLifecycleRules
{
    private static readonly HashSet<string> ValidReferenceTypes = ["VOUCHER", "PRODUCT", "BLOG", "SALE", "OTHER"];
    private const int MinLeadMinutes = 30;
    private const int MaxFutureDays = 90;
    private static readonly TimeSpan VoucherEndBuffer = TimeSpan.FromHours(2);
    private static readonly TimeSpan SaleLeadWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan VoucherSoonWarn = TimeSpan.FromHours(24);

    private readonly IUnitOfWork _uow;
    private readonly ITimeProvider _time;

    public CampaignLifecycleRules(IUnitOfWork unitOfWork, ITimeProvider timeProvider)
    {
        _uow  = unitOfWork;
        _time = timeProvider;
    }

    public async Task<Result?> ValidateCreateDtoAsync(CreateCampaignDto dto, CancellationToken ct = default)
    {
        if (!string.Equals(dto.SourceType, "ADMIN", StringComparison.OrdinalIgnoreCase))
            return Result.Failure(CampaignErrorCodes.SourceTypeInvalid, "Staff campaigns must use SourceType ADMIN.");

        if (dto.ScheduledAt.HasValue)
            return Result.Failure(CampaignErrorCodes.ScheduleNotAllowedAtCreate, "ScheduledAt must not be set when creating a campaign.");

        if (!HasValidContent(dto.TemplateCode, dto.TitleOverride, dto.MessageOverride))
            return Result.Failure(CampaignErrorCodes.ContentRequired, "Template code or both title and message overrides are required.");

        if (!string.IsNullOrWhiteSpace(dto.TemplateCode))
        {
            var tmpl = await _uow.Templates.GetActiveByCodeAsync(dto.TemplateCode.Trim(), ct);
            if (tmpl is null)
                return Result.Failure(CampaignErrorCodes.TemplateNotFound, "Template is missing, inactive, or deleted.");
        }

        var refErr = ReferencePairRule(dto.ReferenceType, dto.ReferenceId);
        if (refErr is not null) return refErr;

        if (!string.IsNullOrWhiteSpace(dto.ReferenceType))
        {
            var entityErr = await ValidateReferenceEntityExistsAsync(dto.ReferenceType!, dto.ReferenceId!.Value, ct);
            if (entityErr is not null) return entityErr;
        }

        if (dto.TargetType is "INDIVIDUAL" or "ROLE")
        {
            if (dto.Targets is null || dto.Targets.Count == 0)
                return Result.Failure(CampaignErrorCodes.TargetRequired, "At least one target is required.");

            if (dto.TargetType == "INDIVIDUAL")
            {
                var ids = dto.Targets
                    .Where(t => t.TargetType == "ACCOUNT_ID")
                    .Select(t => int.TryParse(t.TargetValue, out var id) ? id : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (ids.Count == 0)
                    return Result.Failure(CampaignErrorCodes.TargetRequired, "INDIVIDUAL requires ACCOUNT_ID targets.");

                var count = await _uow.Accounts.CountActiveCustomersByIdsAsync(ids, ct);
                if (count != ids.Count)
                    return Result.Failure(CampaignErrorCodes.TargetAccountInvalid, "One or more account targets are invalid or inactive.");
            }
        }

        return null;
    }

    public async Task<Result?> ValidateUpdateDtoAsync(UpdateCampaignDto dto, CancellationToken ct = default)
    {
        if (dto.ScheduledAt.HasValue)
            return Result.Failure(CampaignErrorCodes.ScheduleNotAllowedAtCreate, "ScheduledAt must not be set when editing; use the schedule endpoint.");

        if (!HasValidContent(dto.TemplateCode, dto.TitleOverride, dto.MessageOverride))
            return Result.Failure(CampaignErrorCodes.ContentRequired, "Template code or both title and message overrides are required.");

        if (!string.IsNullOrWhiteSpace(dto.TemplateCode))
        {
            var tmpl = await _uow.Templates.GetActiveByCodeAsync(dto.TemplateCode.Trim(), ct);
            if (tmpl is null)
                return Result.Failure(CampaignErrorCodes.TemplateNotFound, "Template is missing, inactive, or deleted.");
        }

        var refErr = ReferencePairRule(dto.ReferenceType, dto.ReferenceId);
        if (refErr is not null) return refErr;

        if (!string.IsNullOrWhiteSpace(dto.ReferenceType))
        {
            var entityErr = await ValidateReferenceEntityExistsAsync(dto.ReferenceType!, dto.ReferenceId!.Value, ct);
            if (entityErr is not null) return entityErr;
        }

        if (dto.TargetType is "INDIVIDUAL" or "ROLE")
        {
            if (dto.Targets is null || dto.Targets.Count == 0)
                return Result.Failure(CampaignErrorCodes.TargetRequired, "At least one target is required.");

            if (dto.TargetType == "INDIVIDUAL")
            {
                var ids = dto.Targets
                    .Where(t => t.TargetType == "ACCOUNT_ID")
                    .Select(t => int.TryParse(t.TargetValue, out var id) ? id : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (ids.Count == 0)
                    return Result.Failure(CampaignErrorCodes.TargetRequired, "INDIVIDUAL requires ACCOUNT_ID targets.");

                var count = await _uow.Accounts.CountActiveCustomersByIdsAsync(ids, ct);
                if (count != ids.Count)
                    return Result.Failure(CampaignErrorCodes.TargetAccountInvalid, "One or more account targets are invalid or inactive.");
            }
        }

        return null;
    }

    public async Task<Result> ValidateSubmitAsync(Campaign campaign, int actorAccountId, CancellationToken ct = default)
    {
        if (campaign.CreatedByAccountId != actorAccountId)
            return Result.Failure(CampaignErrorCodes.Forbidden, "Only the campaign creator can submit.");

        if (campaign.Status is not ("Draft" or "Rejected"))
            return Result.Failure(CampaignErrorCodes.InvalidStatusTransition, "Campaign must be Draft or Rejected to submit.");

        if (!HasValidContent(campaign.TemplateCode, campaign.TitleOverride, campaign.MessageOverride))
            return Result.Failure(CampaignErrorCodes.ContentRequired, "Content is required.");

        if (!string.IsNullOrWhiteSpace(campaign.TemplateCode))
        {
            var tmpl = await _uow.Templates.GetActiveByCodeAsync(campaign.TemplateCode.Trim(), ct);
            if (tmpl is null)
                return Result.Failure(CampaignErrorCodes.TemplateNotFound, "Template is missing, inactive, or deleted.");
        }

        if (campaign.TargetType != "ALL" && (campaign.CampaignTargets == null || campaign.CampaignTargets.Count == 0))
            return Result.Failure(CampaignErrorCodes.TargetRequired, "Targets are required when TargetType is not ALL.");

        if (campaign.TargetType == "INDIVIDUAL")
        {
            var ids = campaign.CampaignTargets!
                .Where(t => t.TargetType == "ACCOUNT_ID")
                .Select(t => int.TryParse(t.TargetValue, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var count = await _uow.Accounts.CountActiveCustomersByIdsAsync(ids, ct);
            if (count != ids.Count)
                return Result.Failure(CampaignErrorCodes.TargetAccountInvalid, "One or more account targets are invalid.");
        }

        var refCheck = await ValidateReferenceForSubmitAsync(campaign.ReferenceType, campaign.ReferenceId, ct);
        if (refCheck is not null) return refCheck;

        if (campaign.ScheduledAt.HasValue || campaign.ValidFrom.HasValue || campaign.ValidTo.HasValue)
            return Result.Failure(CampaignErrorCodes.ScheduleNotAllowedAtSubmit, "Schedule fields must be empty at submit.");

        return Result.Success();
    }

    public async Task<Result> ValidateApproveAsync(
        Campaign campaign,
        int reviewerAccountId,
        bool reviewerIsAdmin = false,
        CancellationToken ct = default)
    {
        if (campaign.Status != "PendingApproval")
            return Result.Failure(CampaignErrorCodes.InvalidStatusTransition, "Campaign is not pending approval.");

        if (!reviewerIsAdmin && campaign.SubmittedByAccountId == reviewerAccountId)
            return Result.Failure(CampaignErrorCodes.ReviewerCannotBeSubmitter, "Reviewer cannot be the submitter.");

        var refActive = await ValidateReferenceStillActiveAsync(campaign.ReferenceType, campaign.ReferenceId, requireFutureEndForVoucherAndSale: true, ct);
        if (refActive is not null) return refActive;

        if (!HasValidContent(campaign.TemplateCode, campaign.TitleOverride, campaign.MessageOverride))
            return Result.Failure(CampaignErrorCodes.ContentRequired, "Content is required.");

        if (!string.IsNullOrWhiteSpace(campaign.TemplateCode))
        {
            var tmpl = await _uow.Templates.GetActiveByCodeAsync(campaign.TemplateCode.Trim(), ct);
            if (tmpl is null)
                return Result.Failure(CampaignErrorCodes.TemplateDeactivated, "Template was deactivated.");
        }

        return Result.Success();
    }

    public Task<Result> ValidateRejectAsync(
        Campaign campaign,
        int reviewerAccountId,
        string? reviewNote,
        bool reviewerIsAdmin = false,
        CancellationToken ct = default)
    {
        if (campaign.Status != "PendingApproval")
            return Task.FromResult(Result.Failure(CampaignErrorCodes.InvalidStatusTransition, "Campaign is not pending approval."));

        if (!reviewerIsAdmin && campaign.SubmittedByAccountId == reviewerAccountId)
            return Task.FromResult(Result.Failure(CampaignErrorCodes.ReviewerCannotBeSubmitter, "Reviewer cannot be the submitter."));

        if (string.IsNullOrWhiteSpace(reviewNote) || reviewNote.Trim().Length > 500)
            return Task.FromResult(Result.Failure(CampaignErrorCodes.ReviewNoteRequired, "Review note is required (max 500 characters)."));

        return Task.FromResult(Result.Success());
    }

    public Task<Result> ValidateRecallAsync(Campaign campaign, int actorAccountId, CancellationToken ct = default)
    {
        if (campaign.SubmittedByAccountId != actorAccountId)
            return Task.FromResult(Result.Failure(CampaignErrorCodes.Forbidden, "Only the submitter can recall this campaign."));

        if (campaign.Status != "PendingApproval")
            return Task.FromResult(Result.Failure(CampaignErrorCodes.InvalidStatusTransition, "Campaign is not pending recall."));

        return Task.FromResult(Result.Success());
    }

    public async Task<Result> ValidateScheduleAsync(
        Campaign campaign,
        DateTime scheduledAtUtc,
        DateTime? validFromUtc,
        DateTime? validToUtc,
        IList<string> warningCodes,
        CancellationToken ct = default)
    {
        if (campaign.Status != "Approved")
            return Result.Failure(CampaignErrorCodes.InvalidStatusTransition, "Campaign must be Approved to schedule.");

        var now = _time.UtcNow;
        if (campaign.ApprovedExpireAt.HasValue && now > campaign.ApprovedExpireAt.Value)
            return Result.Failure(CampaignErrorCodes.ApprovedExpired, "The approval window to schedule this campaign has expired.");

        if (scheduledAtUtc < now.AddMinutes(MinLeadMinutes))
            return Result.Failure(CampaignErrorCodes.ScheduledAtTooSoon, $"Scheduled time must be at least {MinLeadMinutes} minutes from now.");

        if (scheduledAtUtc > now.AddDays(MaxFutureDays))
            return Result.Failure(CampaignErrorCodes.ScheduledAtTooFar, $"Scheduled time must be within {MaxFutureDays} days.");

        if (validFromUtc.HasValue && validToUtc.HasValue && validFromUtc >= validToUtc)
            return Result.Failure(CampaignErrorCodes.ValidRangeInvalid, "ValidFrom must be before ValidTo.");

        if (validFromUtc.HasValue && validToUtc.HasValue
            && (scheduledAtUtc < validFromUtc.Value || scheduledAtUtc > validToUtc.Value))
            return Result.Failure(CampaignErrorCodes.ScheduledAtOutOfRange, "ScheduledAt must fall within ValidFrom and ValidTo.");

        return await ValidateReferenceScheduleRulesAsync(campaign, scheduledAtUtc, warningCodes, ct);
    }

    public async Task<Result> ValidateRescheduleAsync(
        Campaign campaign,
        DateTime newScheduledAtUtc,
        string? reason,
        IList<string> warningCodes,
        CancellationToken ct = default)
    {
        if (campaign.Status != "Scheduled")
            return Result.Failure(CampaignErrorCodes.InvalidStatusTransition, "Campaign must be Scheduled to reschedule.");

        if (campaign.RescheduleCount >= campaign.MaxRescheduleCount)
            return Result.Failure(CampaignErrorCodes.MaxRescheduleExceeded, "Maximum reschedule count reached.");

        var sched = campaign.CampaignSchedule;
        if (sched is null)
            return Result.Failure(CampaignErrorCodes.InvalidStatusTransition, "Campaign has no schedule row.");

        if (sched.LockedByJobId.HasValue)
            return Result.Failure(CampaignErrorCodes.CampaignLockedByJob, "Campaign is locked by a dispatch job.");

        var now = _time.UtcNow;
        if (newScheduledAtUtc < now.AddMinutes(MinLeadMinutes))
            return Result.Failure(CampaignErrorCodes.ScheduledAtTooSoon, $"New schedule must be at least {MinLeadMinutes} minutes from now.");

        if (sched.ScheduledAt == newScheduledAtUtc)
            return Result.Failure(CampaignErrorCodes.SameScheduledAt, "New schedule must differ from the current schedule.");

        if (!string.IsNullOrEmpty(reason) && reason.Length > 200)
            return Result.Failure(CampaignErrorCodes.ReasonTooLong, "Reason must not exceed 200 characters.");

        return await ValidateReferenceScheduleRulesAsync(campaign, newScheduledAtUtc, warningCodes, ct);
    }

    public Task<Result> ValidateCancelAsync(Campaign campaign, int actorAccountId, bool actorIsAdmin, CancellationToken ct = default)
    {
        if (campaign.Status is not ("Draft" or "Approved" or "Scheduled"))
            return Task.FromResult(Result.Failure(CampaignErrorCodes.InvalidStatusTransition, "Campaign cannot be cancelled in this status."));

        if (campaign.Status == "Scheduled" && campaign.CampaignSchedule?.LockedByJobId is not null)
            return Task.FromResult(Result.Failure(CampaignErrorCodes.CampaignLockedByJob, "Campaign is locked for dispatch."));

        if (!actorIsAdmin && campaign.CreatedByAccountId != actorAccountId)
            return Task.FromResult(Result.Failure(CampaignErrorCodes.Forbidden, "You can only cancel your own campaigns."));

        return Task.FromResult(Result.Success());
    }

    public bool ShouldSkipDispatch(Campaign campaign, DateTime nowUtc, out string? logReason)
    {
        logReason = null;
        if (campaign.Status != "Scheduled") { logReason = "not Scheduled"; return true; }
        if (campaign.IsDeleted) { logReason = "deleted"; return true; }
        var sched = campaign.CampaignSchedule;
        if (sched is null) { logReason = "no schedule"; return true; }
        if (sched.ScheduledAt > nowUtc) { logReason = "not due"; return true; }
        if (sched.ExecutionStatus != "Waiting") { logReason = $"execution {sched.ExecutionStatus}"; return true; }
        if (sched.LockedByJobId.HasValue) { logReason = "already locked"; return true; }
        if (sched.AttemptCount >= sched.MaxAttemptCount) { logReason = "max attempts"; return true; }
        return false;
    }

    public async Task<Result> ValidateDispatchAsync(Campaign campaign, CancellationToken ct = default)
    {
        if (!HasValidContent(campaign.TemplateCode, campaign.TitleOverride, campaign.MessageOverride))
            return Result.Failure(CampaignErrorCodes.ContentRequired, "Campaign content is incomplete.");

        if (!string.IsNullOrWhiteSpace(campaign.TemplateCode))
        {
            var tmpl = await _uow.Templates.GetActiveByCodeAsync(campaign.TemplateCode.Trim(), ct);
            if (tmpl is null)
                return Result.Failure(CampaignErrorCodes.TemplateDeactivated, "Template was deactivated.");
        }

        if (!string.IsNullOrWhiteSpace(campaign.ReferenceType))
        {
            var live = campaign.CampaignReferenceSnapshots.FirstOrDefault(s => !s.IsStale);
            if (live is null)
                return Result.Failure(CampaignErrorCodes.ReferenceNotFound, "Missing live reference snapshot.");
        }

        var refActive = await ValidateReferenceStillActiveAsync(
            campaign.ReferenceType,
            campaign.ReferenceId,
            requireFutureEndForVoucherAndSale: true,
            ct);

        return refActive is not null ? refActive : Result.Success();
    }

    public async Task<CampaignReferenceSnapshot?> BuildLiveReferenceSnapshotAsync(Campaign campaign, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(campaign.ReferenceType) || !campaign.ReferenceId.HasValue)
            return null;

        var rt = campaign.ReferenceType.Trim().ToUpperInvariant();
        var id = campaign.ReferenceId.Value;
        var now = _time.UtcNow;

        return rt switch
        {
            "VOUCHER" => await BuildVoucherSnapshotAsync(campaign.CampaignId, id, ct),
            "SALE"     => await BuildSaleSnapshotAsync(campaign.CampaignId, id, ct),
            "PRODUCT"  => await BuildProductSnapshotAsync(campaign.CampaignId, id, ct),
            "BLOG"     => await BuildBlogSnapshotAsync(campaign.CampaignId, id, ct),
            "OTHER"    => new CampaignReferenceSnapshot
            {
                CampaignId    = campaign.CampaignId,
                ReferenceType = rt,
                ReferenceId   = id,
                EntityStatus  = "OTHER",
                EntityStartDate = null,
                EntityEndDate   = now.AddYears(10),
                IsStale       = false,
                SnapshotAt    = now
            },
            _ => null
        };
    }

    // ── internals ──────────────────────────────────────────────────────

    private async Task<CampaignReferenceSnapshot?> BuildVoucherSnapshotAsync(int campaignId, int voucherId, CancellationToken ct)
    {
        var v = await _uow.Vouchers.GetByIdAsync(voucherId, ct);
        if (v is null || v.IsDeleted) return null;
        var now = _time.UtcNow;
        return new CampaignReferenceSnapshot
        {
            CampaignId      = campaignId,
            ReferenceType   = "VOUCHER",
            ReferenceId     = voucherId,
            EntityStatus    = v.Status,
            EntityStartDate = v.StartDate,
            EntityEndDate   = v.EndDate,
            IsStale         = false,
            SnapshotAt      = now
        };
    }

    private async Task<CampaignReferenceSnapshot?> BuildSaleSnapshotAsync(int campaignId, int promoId, CancellationToken ct)
    {
        var p = await _uow.Promotions.GetByIdAsync(promoId, ct, "PromotionTimeSlots");
        if (p is null) return null;
        var now = _time.UtcNow;
        return new CampaignReferenceSnapshot
        {
            CampaignId      = campaignId,
            ReferenceType   = "SALE",
            ReferenceId     = promoId,
            EntityStatus    = p.Status,
            EntityStartDate = p.StartDate,
            EntityEndDate   = p.EndDate,
            IsStale         = false,
            SnapshotAt      = now
        };
    }

    private async Task<CampaignReferenceSnapshot?> BuildProductSnapshotAsync(int campaignId, int productId, CancellationToken ct)
    {
        var p = await _uow.Products.GetByIdAsync(productId, ct);
        if (p is null || p.IsDeleted) return null;
        var now = _time.UtcNow;
        var end = p.LaunchDate?.AddYears(5) ?? now.AddYears(10);
        return new CampaignReferenceSnapshot
        {
            CampaignId      = campaignId,
            ReferenceType   = "PRODUCT",
            ReferenceId     = productId,
            EntityStatus    = p.ProductStatus,
            EntityStartDate = null,
            EntityEndDate   = end,
            IsStale         = false,
            SnapshotAt      = now
        };
    }

    private async Task<CampaignReferenceSnapshot?> BuildBlogSnapshotAsync(int campaignId, int blogId, CancellationToken ct)
    {
        var b = await _uow.Blogs.GetByIdAsync(blogId, ct);
        if (b is null || b.IsDeleted) return null;
        var now = _time.UtcNow;
        return new CampaignReferenceSnapshot
        {
            CampaignId      = campaignId,
            ReferenceType   = "BLOG",
            ReferenceId     = blogId,
            EntityStatus    = b.Status,
            EntityStartDate = b.BlogAt,
            EntityEndDate   = now.AddYears(10),
            IsStale         = false,
            SnapshotAt      = now
        };
    }

    private async Task<Result> ValidateReferenceScheduleRulesAsync(
        Campaign campaign,
        DateTime scheduledAtUtc,
        IList<string> warningCodes,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(campaign.ReferenceType))
            return Result.Success();

        var rt = campaign.ReferenceType.Trim().ToUpperInvariant();

        return rt switch
        {
            "VOUCHER" => await ValidateVoucherScheduleAsync(campaign.ReferenceId!.Value, scheduledAtUtc, warningCodes, ct),
            "SALE"    => await ValidateSaleScheduleAsync(campaign.ReferenceId!.Value, scheduledAtUtc, ct),
            "PRODUCT" => await ValidateProductScheduleAsync(campaign.ReferenceId!.Value, scheduledAtUtc, ct),
            "BLOG"    => await ValidateBlogScheduleAsync(campaign.ReferenceId!.Value, ct),
            "OTHER"   => Result.Success(),
            _         => Result.Success()
        };
    }

    private async Task<Result> ValidateVoucherScheduleAsync(int voucherId, DateTime scheduledAtUtc, IList<string> warningCodes, CancellationToken ct)
    {
        var v = await _uow.Vouchers.GetByIdAsync(voucherId, ct);
        if (v is null || v.IsDeleted || v.Status != "Active")
            return Result.Failure(CampaignErrorCodes.ReferenceNotFound, "Voucher not found or not active.");

        if (scheduledAtUtc < v.StartDate)
            return Result.Failure(CampaignErrorCodes.ScheduledBeforeVoucherStart, "Scheduled time must be on or after voucher start.");

        if (scheduledAtUtc > v.EndDate - VoucherEndBuffer)
            return Result.Failure(CampaignErrorCodes.ScheduledTooCloseToVoucherEnd, "Scheduled time must be at least 2 hours before voucher end.");

        if (v.EndDate - scheduledAtUtc < VoucherSoonWarn)
            warningCodes.Add(CampaignErrorCodes.WarnVoucherExpiringSoon);

        return Result.Success();
    }

    private async Task<Result> ValidateSaleScheduleAsync(int promoId, DateTime scheduledAtUtc, CancellationToken ct)
    {
        var p = await _uow.Promotions.GetByIdAsync(promoId, ct, "PromotionTimeSlots");
        if (p is null || p.IsDeleted || p.Status is not ("Active" or "Scheduled"))
            return Result.Failure(CampaignErrorCodes.ReferenceNotFound, "Promotion not found or not active/scheduled.");

        var earliest = p.StartDate - SaleLeadWindow;
        if (scheduledAtUtc < earliest)
            return Result.Failure(CampaignErrorCodes.ScheduledTooEarlyForSale, "Scheduled time must not be more than 24 hours before sale start.");

        if (string.Equals(p.PromotionType, "FLASH_SALE", StringComparison.OrdinalIgnoreCase))
        {
            var activeSlots = p.PromotionTimeSlots.Where(s => !s.IsDeleted).ToList();
            if (activeSlots.Count == 0)
                return Result.Failure(CampaignErrorCodes.ReferenceNotFound, "Flash sale has no time slots.");

            // Align with non-flash sale: keep a buffer before the promotion window ends. For flash, the
            // meaningful window end is the last slot's EndAt (multi-slot / multi-day friendly).
            var lastSlotEndUtc = activeSlots.Max(s => s.EndAt);
            if (scheduledAtUtc > lastSlotEndUtc - VoucherEndBuffer)
                return Result.Failure(
                    CampaignErrorCodes.ScheduledTooCloseToSaleEnd,
                    "Scheduled time must be at least 2 hours before the last flash sale time slot ends.");
        }
        else
        {
            if (scheduledAtUtc > p.EndDate - VoucherEndBuffer)
                return Result.Failure(CampaignErrorCodes.ScheduledTooCloseToSaleEnd, "Scheduled time must be at least 2 hours before promotion end.");
        }

        return Result.Success();
    }

    private async Task<Result> ValidateProductScheduleAsync(int productId, DateTime scheduledAtUtc, CancellationToken ct)
    {
        var p = await _uow.Products.GetByIdAsync(productId, ct);
        if (p is null || p.IsDeleted)
            return Result.Failure(CampaignErrorCodes.ReferenceNotFound, "Product not found.");

        if (string.Equals(p.ProductStatus, "ComingSoon", StringComparison.OrdinalIgnoreCase) && p.LaunchDate.HasValue
            && scheduledAtUtc > p.LaunchDate.Value)
            return Result.Failure(CampaignErrorCodes.ScheduledAfterLaunch, "For coming-soon products, schedule must be on or before launch.");

        return Result.Success();
    }

    private async Task<Result> ValidateBlogScheduleAsync(int blogId, CancellationToken ct)
    {
        var b = await _uow.Blogs.GetByIdAsync(blogId, ct);
        if (b is null || b.IsDeleted || b.Status != "Published")
            return Result.Failure(CampaignErrorCodes.ReferenceNotFound, "Blog not found or not published.");

        return Result.Success();
    }

    private static Result? ReferencePairRule(string? referenceType, int? referenceId)
    {
        var hasType = !string.IsNullOrWhiteSpace(referenceType);
        var hasId   = referenceId.HasValue && referenceId.Value > 0;

        if (hasType != hasId)
            return Result.Failure(CampaignErrorCodes.ReferenceInconsistent, "ReferenceType and ReferenceID must both be set or both empty.");

        if (hasType && !ValidReferenceTypes.Contains(referenceType!.Trim().ToUpperInvariant()))
            return Result.Failure(CampaignErrorCodes.ReferenceInconsistent, "Invalid reference type.");

        return null;
    }

    private static bool HasValidContent(string? templateCode, string? titleOverride, string? messageOverride)
    {
        if (!string.IsNullOrWhiteSpace(templateCode)) return true;
        return !string.IsNullOrWhiteSpace(titleOverride) && !string.IsNullOrWhiteSpace(messageOverride);
    }

    private async Task<Result?> ValidateReferenceEntityExistsAsync(string referenceType, int referenceId, CancellationToken ct)
    {
        var rt = referenceType.Trim().ToUpperInvariant();
        if (rt == "OTHER") return null;

        return rt switch
        {
            "VOUCHER" => await VoucherMeetsActiveRowAsync(
                referenceId,
                requireEndDateInFuture: true,
                ct) ? null : Result.Failure(CampaignErrorCodes.ReferenceNotFound, "Referenced voucher not found."),
            "SALE" => await SaleMeetsActiveRowAsync(referenceId, ct)
                ? null
                : Result.Failure(CampaignErrorCodes.ReferenceNotFound, "Referenced promotion not found."),
            "PRODUCT" => await ProductMeetsRowAsync(referenceId, ct)
                ? null
                : Result.Failure(CampaignErrorCodes.ReferenceNotFound, "Referenced product not found."),
            "BLOG" => await BlogMeetsPublishedAsync(referenceId, ct)
                ? null
                : Result.Failure(CampaignErrorCodes.ReferenceNotFound, "Referenced blog not found."),
            _ => Result.Failure(CampaignErrorCodes.ReferenceInconsistent, "Invalid reference type.")
        };
    }

    private async Task<Result?> ValidateReferenceForSubmitAsync(string? referenceType, int? referenceId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(referenceType)) return null;
        return await ValidateReferenceStillActiveAsync(referenceType, referenceId, requireFutureEndForVoucherAndSale: false, ct);
    }

    private async Task<Result?> ValidateReferenceStillActiveAsync(
        string? referenceType,
        int? referenceId,
        bool requireFutureEndForVoucherAndSale,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(referenceType) || !referenceId.HasValue) return null;

        var rt = referenceType.Trim().ToUpperInvariant();
        var now = _time.UtcNow;

        if (rt == "OTHER") return null;

        if (rt == "VOUCHER")
        {
            var v = await _uow.Vouchers.GetByIdAsync(referenceId.Value, ct);
            if (v is null || v.IsDeleted || v.Status != "Active")
                return Result.Failure(CampaignErrorCodes.ReferenceExpired, "Voucher reference is no longer valid.");

            if (requireFutureEndForVoucherAndSale && v.EndDate <= now)
                return Result.Failure(CampaignErrorCodes.ReferenceAlreadyExpired, "Voucher has already expired.");

            return null;
        }

        if (rt == "SALE")
        {
            var p = await _uow.Promotions.GetByIdAsync(referenceId.Value, ct);
            if (p is null || p.IsDeleted || p.Status is not ("Active" or "Scheduled"))
                return Result.Failure(CampaignErrorCodes.ReferenceExpired, "Promotion reference is no longer valid.");

            if (requireFutureEndForVoucherAndSale && p.EndDate <= now)
                return Result.Failure(CampaignErrorCodes.ReferenceAlreadyExpired, "Promotion has already ended.");

            return null;
        }

        if (rt == "PRODUCT")
        {
            var p = await _uow.Products.GetByIdAsync(referenceId.Value, ct);
            if (p is null || p.IsDeleted || string.Equals(p.ProductStatus, "Discontinued", StringComparison.OrdinalIgnoreCase))
                return Result.Failure(CampaignErrorCodes.ReferenceExpired, "Product reference is no longer valid.");
            return null;
        }

        if (rt == "BLOG")
        {
            var b = await _uow.Blogs.GetByIdAsync(referenceId.Value, ct);
            if (b is null || b.IsDeleted || b.Status != "Published")
                return Result.Failure(CampaignErrorCodes.ReferenceExpired, "Blog reference is no longer valid.");
            return null;
        }

        return null;
    }

    private async Task<bool> VoucherMeetsActiveRowAsync(int id, bool requireEndDateInFuture, CancellationToken ct)
    {
        var v = await _uow.Vouchers.GetByIdAsync(id, ct);
        if (v is null || v.IsDeleted || v.Status != "Active") return false;
        if (requireEndDateInFuture && v.EndDate <= _time.UtcNow) return false;
        return true;
    }

    private async Task<bool> SaleMeetsActiveRowAsync(int id, CancellationToken ct)
    {
        var p = await _uow.Promotions.GetByIdAsync(id, ct);
        return p is { IsDeleted: false, Status: "Active" or "Scheduled", EndDate: var end }
               && end > _time.UtcNow;
    }

    private async Task<bool> ProductMeetsRowAsync(int id, CancellationToken ct)
    {
        var p = await _uow.Products.GetByIdAsync(id, ct);
        return p is { IsDeleted: false, ProductStatus: not "Discontinued" };
    }

    private async Task<bool> BlogMeetsPublishedAsync(int id, CancellationToken ct)
    {
        var b = await _uow.Blogs.GetByIdAsync(id, ct);
        return b is { IsDeleted: false, Status: "Published" };
    }

    public async Task<Result<CampaignScheduleBoundsDto>> GetScheduleSendWindowBoundsAsync(
        Campaign campaign,
        CancellationToken ct = default)
    {
        var now = _time.UtcNow;
        var gMin = now.AddMinutes(MinLeadMinutes);
        var gMax = now.AddDays(MaxFutureDays);

        var dto = new CampaignScheduleBoundsDto
        {
            ReferenceType = string.IsNullOrWhiteSpace(campaign.ReferenceType)
                ? null
                : campaign.ReferenceType.Trim().ToUpperInvariant(),
            EarliestUtc = gMin,
            LatestUtc = gMax,
            IsFeasible = true,
            ReferenceRulesApplied = false,
        };

        if (string.IsNullOrWhiteSpace(campaign.ReferenceType) || campaign.ReferenceId is null or <= 0)
        {
            dto.IsFeasible = gMin <= gMax;
            return Result<CampaignScheduleBoundsDto>.Success(dto);
        }

        var rt = campaign.ReferenceType.Trim().ToUpperInvariant();
        dto.ReferenceType = rt;

        switch (rt)
        {
            case "OTHER":
            case "BLOG":
                dto.IsFeasible = gMin <= gMax;
                return Result<CampaignScheduleBoundsDto>.Success(dto);

            case "VOUCHER":
                return await BuildVoucherScheduleBoundsAsync(dto, campaign.ReferenceId.Value, gMin, gMax, ct);

            case "SALE":
                return await BuildSaleScheduleBoundsAsync(dto, campaign.ReferenceId.Value, gMin, gMax, ct);

            case "PRODUCT":
                return await BuildProductScheduleBoundsAsync(dto, campaign.ReferenceId.Value, gMin, gMax, ct);

            default:
                dto.ReferenceHintWarning = "Loại reference không hỗ trợ tính khung giờ chi tiết.";
                dto.IsFeasible = gMin <= gMax;
                return Result<CampaignScheduleBoundsDto>.Success(dto);
        }
    }

    private async Task<Result<CampaignScheduleBoundsDto>> BuildVoucherScheduleBoundsAsync(
        CampaignScheduleBoundsDto dto,
        int voucherId,
        DateTime gMin,
        DateTime gMax,
        CancellationToken ct)
    {
        var v = await _uow.Vouchers.GetByIdAsync(voucherId, ct);
        if (v is null || v.IsDeleted || v.Status != "Active")
        {
            dto.ReferenceHintWarning =
                "Không tìm thấy voucher Active — chỉ hiển thị giới hạn chung hệ thống (sau 30 phút, trong 90 ngày).";
            dto.EarliestUtc = gMin;
            dto.LatestUtc = gMax;
            dto.IsFeasible = gMin <= gMax;
            return Result<CampaignScheduleBoundsDto>.Success(dto);
        }

        var rMin = v.StartDate;
        var rMax = v.EndDate - VoucherEndBuffer;
        dto.ReferenceRulesApplied = true;
        dto.EarliestUtc = MaxDt(gMin, rMin);
        dto.LatestUtc = MinDt(gMax, rMax);
        dto.IsFeasible = dto.EarliestUtc <= dto.LatestUtc;
        return Result<CampaignScheduleBoundsDto>.Success(dto);
    }

    private async Task<Result<CampaignScheduleBoundsDto>> BuildSaleScheduleBoundsAsync(
        CampaignScheduleBoundsDto dto,
        int promoId,
        DateTime gMin,
        DateTime gMax,
        CancellationToken ct)
    {
        var p = await _uow.Promotions.GetByIdAsync(promoId, ct, "PromotionTimeSlots");
        if (p is null || p.IsDeleted || p.Status is not ("Active" or "Scheduled"))
        {
            dto.ReferenceHintWarning =
                "Không tìm thấy khuyến mãi Active/Scheduled — chỉ hiển thị giới hạn chung hệ thống.";
            dto.EarliestUtc = gMin;
            dto.LatestUtc = gMax;
            dto.IsFeasible = gMin <= gMax;
            return Result<CampaignScheduleBoundsDto>.Success(dto);
        }

        dto.PromotionType = p.PromotionType;
        var rMin = p.StartDate - SaleLeadWindow;
        DateTime rMax;
        if (string.Equals(p.PromotionType, "FLASH_SALE", StringComparison.OrdinalIgnoreCase))
        {
            var activeSlots = p.PromotionTimeSlots.Where(s => !s.IsDeleted).ToList();
            if (activeSlots.Count == 0)
            {
                dto.ReferenceHintWarning = "Flash sale không có time slot — chỉ hiển thị giới hạn chung.";
                dto.EarliestUtc = gMin;
                dto.LatestUtc = gMax;
                dto.IsFeasible = gMin <= gMax;
                return Result<CampaignScheduleBoundsDto>.Success(dto);
            }

            rMax = activeSlots.Max(s => s.EndAt) - VoucherEndBuffer;
        }
        else
            rMax = p.EndDate - VoucherEndBuffer;

        dto.ReferenceRulesApplied = true;
        dto.EarliestUtc = MaxDt(gMin, rMin);
        dto.LatestUtc = MinDt(gMax, rMax);
        dto.IsFeasible = dto.EarliestUtc <= dto.LatestUtc;
        return Result<CampaignScheduleBoundsDto>.Success(dto);
    }

    private async Task<Result<CampaignScheduleBoundsDto>> BuildProductScheduleBoundsAsync(
        CampaignScheduleBoundsDto dto,
        int productId,
        DateTime gMin,
        DateTime gMax,
        CancellationToken ct)
    {
        var p = await _uow.Products.GetByIdAsync(productId, ct);
        if (p is null || p.IsDeleted)
        {
            dto.ReferenceHintWarning = "Không tìm thấy sản phẩm — chỉ hiển thị giới hạn chung.";
            dto.EarliestUtc = gMin;
            dto.LatestUtc = gMax;
            dto.IsFeasible = gMin <= gMax;
            return Result<CampaignScheduleBoundsDto>.Success(dto);
        }

        if (string.Equals(p.ProductStatus, "ComingSoon", StringComparison.OrdinalIgnoreCase) && p.LaunchDate.HasValue)
        {
            dto.ReferenceRulesApplied = true;
            var rMax = p.LaunchDate.Value;
            dto.EarliestUtc = gMin;
            dto.LatestUtc = MinDt(gMax, rMax);
            dto.IsFeasible = dto.EarliestUtc <= dto.LatestUtc;
            return Result<CampaignScheduleBoundsDto>.Success(dto);
        }

        dto.EarliestUtc = gMin;
        dto.LatestUtc = gMax;
        dto.IsFeasible = gMin <= gMax;
        return Result<CampaignScheduleBoundsDto>.Success(dto);
    }

    private static DateTime MaxDt(DateTime a, DateTime b) => a >= b ? a : b;

    private static DateTime MinDt(DateTime a, DateTime b) => a <= b ? a : b;
}
