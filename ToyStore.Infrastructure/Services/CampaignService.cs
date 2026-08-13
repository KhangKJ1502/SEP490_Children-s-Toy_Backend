using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Services.Resolvers;

namespace ToyStore.Infrastructure.Services;

public class CampaignService : ICampaignService
{
    private static readonly HashSet<string> ValidStatuses =
        ["Draft", "PendingApproval", "Approved", "Rejected", "Scheduled", "Sending", "Sent", "Cancelled", "Failed"];
    private static readonly HashSet<string> ValidSourceTypes = ["ADMIN", "SYSTEM"];
    private static readonly HashSet<string> ValidSortFields = ["createdat", "name", "status"];
    private static readonly HashSet<string> EditableStatuses = ["Draft", "Rejected"];

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CampaignService> _logger;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateCampaignDto> _createValidator;
    private readonly IValidator<UpdateCampaignDto> _updateValidator;
    private readonly IValidator<ReviewCampaignDto> _reviewValidator;
    private readonly IValidator<ScheduleCampaignDto> _scheduleValidator;
    private readonly IValidator<RescheduleCampaignDto> _rescheduleValidator;
    private readonly BusinessObjectResolverFactory _resolverFactory;
    private readonly ITimeProvider _timeProvider;
    private readonly ICampaignLifecycleRules _rules;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ICurrentUserService _currentUser;

    public CampaignService(
        IUnitOfWork unitOfWork,
        ILogger<CampaignService> logger,
        IMapper mapper,
        IValidator<CreateCampaignDto> createValidator,
        IValidator<UpdateCampaignDto> updateValidator,
        IValidator<ReviewCampaignDto> reviewValidator,
        IValidator<ScheduleCampaignDto> scheduleValidator,
        IValidator<RescheduleCampaignDto> rescheduleValidator,
        BusinessObjectResolverFactory resolverFactory,
        ITimeProvider timeProvider,
        ICampaignLifecycleRules rules,
        INotificationDispatcher notificationDispatcher,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _reviewValidator = reviewValidator;
        _scheduleValidator = scheduleValidator;
        _rescheduleValidator = rescheduleValidator;
        _resolverFactory = resolverFactory;
        _timeProvider = timeProvider;
        _rules = rules;
        _notificationDispatcher = notificationDispatcher;
        _currentUser = currentUser;
    }

    public async Task<Result<PaginatedResponse<CampaignListDto>>> GetCampaignsAsync(
        CampaignQueryDto query,
        bool viewerIsAdmin,
        int viewerAccountId,
        CancellationToken cancellationToken = default)
    {
        if (query.PageNumber < 1)
            return Result<PaginatedResponse<CampaignListDto>>.Failure(
                "VALIDATION_ERROR", "Page number must be greater than 0.");

        if (query.PageSize < 1 || query.PageSize > 100)
            return Result<PaginatedResponse<CampaignListDto>>.Failure(
                "VALIDATION_ERROR", "Page size must be between 1 and 100.");

        if (!string.IsNullOrWhiteSpace(query.Status) && !ValidStatuses.Contains(query.Status))
            return Result<PaginatedResponse<CampaignListDto>>.Failure(
                "VALIDATION_ERROR",
                $"Invalid status '{query.Status}'. Allowed: {string.Join(", ", ValidStatuses)}.");

        if (!string.IsNullOrWhiteSpace(query.SourceType) && !ValidSourceTypes.Contains(query.SourceType))
            return Result<PaginatedResponse<CampaignListDto>>.Failure(
                "VALIDATION_ERROR",
                $"Invalid sourceType '{query.SourceType}'. Allowed: {string.Join(", ", ValidSourceTypes)}.");

        if (!string.IsNullOrWhiteSpace(query.SortBy) &&
            !ValidSortFields.Contains(query.SortBy.Trim().ToLowerInvariant()))
            return Result<PaginatedResponse<CampaignListDto>>.Failure(
                "VALIDATION_ERROR",
                $"Invalid sortBy '{query.SortBy}'. Allowed: createdAt, name, status.");

        if (query.StartDate.HasValue && query.EndDate.HasValue &&
            query.StartDate.Value.Date > query.EndDate.Value.Date)
            return Result<PaginatedResponse<CampaignListDto>>.Failure(
                "VALIDATION_ERROR", "StartDate must be less than or equal to EndDate.");

        DateTime? startDateUtc = query.StartDate.HasValue
            ? _timeProvider.ToUtc(query.StartDate.Value.Date)
            : null;
        DateTime? endDateUtc = query.EndDate.HasValue
            ? _timeProvider.ToUtc(query.EndDate.Value.Date.AddDays(1))
            : null;

        var items = await _unitOfWork.Campaigns.GetPagedAsync(
            query, startDateUtc, endDateUtc, viewerIsAdmin, viewerAccountId, cancellationToken);
        var totalCount = await _unitOfWork.Campaigns.CountAsync(
            query, startDateUtc, endDateUtc, viewerIsAdmin, viewerAccountId, cancellationToken);
        var mapped = _mapper.Map<List<CampaignListDto>>(items);
        var response = new PaginatedResponse<CampaignListDto>(mapped, totalCount, query.PageNumber, query.PageSize);

        return Result<PaginatedResponse<CampaignListDto>>.Success(response);
    }

    public async Task<Result<CampaignDto>> GetCampaignByIdAsync(
        int campaignId,
        CancellationToken cancellationToken = default)
    {
        if (campaignId <= 0)
            return Result<CampaignDto>.Failure("VALIDATION_ERROR", "Campaign ID must be greater than 0.");

        var campaign = await _unitOfWork.Campaigns.GetByIdAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result<CampaignDto>.NotFound("Campaign", campaignId);

        if (ShouldHideDraftCampaignFromCurrentAdminViewer(campaign))
            return Result<CampaignDto>.NotFound("Campaign", campaignId);

        var dto = _mapper.Map<CampaignDto>(campaign);

        // Enrich with resolved reference data
        if (!string.IsNullOrWhiteSpace(campaign.ReferenceType) && campaign.ReferenceId.HasValue)
        {
            dto.ResolvedReference = await _resolverFactory.ResolveAsync(
                campaign.ReferenceType, campaign.ReferenceId.Value, cancellationToken);
        }

        var tmpl = campaign.TemplateCodeNavigation;
        dto.ResolvedTitle = !string.IsNullOrWhiteSpace(campaign.TitleOverride)
            ? campaign.TitleOverride
            : tmpl?.TitleTemplate;
        dto.ResolvedMessage = !string.IsNullOrWhiteSpace(campaign.MessageOverride)
            ? campaign.MessageOverride
            : tmpl?.MessageTemplate;

        return Result<CampaignDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CampaignScheduleBoundsDto>> GetCampaignScheduleBoundsAsync(
        int campaignId,
        CancellationToken cancellationToken = default)
    {
        if (campaignId <= 0)
            return Result<CampaignScheduleBoundsDto>.Failure(
                "VALIDATION_ERROR", "Campaign ID must be greater than 0.");

        var campaign = await _unitOfWork.Campaigns.GetByIdAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result<CampaignScheduleBoundsDto>.NotFound("Campaign", campaignId);

        if (ShouldHideDraftCampaignFromCurrentAdminViewer(campaign))
            return Result<CampaignScheduleBoundsDto>.NotFound("Campaign", campaignId);

        if (campaign.Status is not ("Approved" or "Scheduled"))
            return Result<CampaignScheduleBoundsDto>.Failure(
                ToyStore.Application.Campaigns.CampaignErrorCodes.InvalidStatusTransition,
                "Schedule bounds are only available for Approved or Scheduled campaigns.");

        return await _rules.GetScheduleSendWindowBoundsAsync(campaign, cancellationToken);
    }

    public async Task<Result<PaginatedResponse<CampaignDeliveryDto>>> GetCampaignDeliveriesAsync(
        int campaignId,
        int pageNumber,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default)
    {
        if (campaignId <= 0)
            return Result<PaginatedResponse<CampaignDeliveryDto>>.Failure(
                "VALIDATION_ERROR", "Campaign ID must be greater than 0.");

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        // Verify campaign exists
        var campaign = await _unitOfWork.Campaigns.GetByIdAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result<PaginatedResponse<CampaignDeliveryDto>>.NotFound("Campaign", campaignId);

        if (ShouldHideDraftCampaignFromCurrentAdminViewer(campaign))
            return Result<PaginatedResponse<CampaignDeliveryDto>>.NotFound("Campaign", campaignId);

        var totalCount = await _unitOfWork.Deliveries.CountByCampaignAsync(campaignId, status, cancellationToken);
        var items = await _unitOfWork.Deliveries.GetByCampaignPagedAsync(campaignId, pageNumber, pageSize, status, cancellationToken);

        var deliveryDtos = items.Select(d => new CampaignDeliveryDto
        {
            DeliveryId = d.DeliveryId,
            AccountId = d.AccountId,
            AccountName = d.Account.AccountName,
            Email = d.Account.Email,
            Status = d.Status,
            Title = d.Title,
            Message = d.Message,
            ReadAt = d.ReadAt,
            CreatedAt = d.CreatedAt,
            IsClicked = d.DeliveryActions.Any(a => string.Equals(a.ActionType, "Click", StringComparison.OrdinalIgnoreCase))
        }).ToList();

        var response = new PaginatedResponse<CampaignDeliveryDto>(deliveryDtos, totalCount, pageNumber, pageSize);
        return Result<PaginatedResponse<CampaignDeliveryDto>>.Success(response);
    }


    public async Task<Result<CampaignDto>> CreateCampaignAsync(
        CreateCampaignDto dto,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<CampaignDto>.ValidationFailure(errors);
        }

        var lifecycleError = await _rules.ValidateCreateDtoAsync(dto, cancellationToken);
        if (lifecycleError is not null)
            return Result<CampaignDto>.Failure(lifecycleError.ErrorCode!, lifecycleError.ErrorMessage!);

        var isDuplicate = await _unitOfWork.Campaigns.ExistsByNameAsync(
            dto.CampaignName.Trim(), cancellationToken: cancellationToken);
        if (isDuplicate)
            return Result<CampaignDto>.Conflict("Campaign name already exists.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.Campaigns.CreateAsync(dto, _timeProvider.UtcNow, cancellationToken);

            await ApplyActionTargetFromResolverAsync(created, cancellationToken);

            // Admin tao campaign thi tu dong duyet — bo qua buoc Submit + Review.
            var creatorIsAdmin = string.Equals(_currentUser.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);
            if (creatorIsAdmin)
            {
                var now = _timeProvider.UtcNow;
                created.Status = "Approved";
                created.ReviewedByAccountId = dto.CreatedByAccountId;
                created.ReviewedAt = now;
                created.ReviewNote = "Auto-approved: created by Admin";
                created.ApprovedExpireAt = now.AddDays(7);
            }

            _unitOfWork.Campaigns.Update(created);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Ghi audit log neu admin tu dong duyet
            if (creatorIsAdmin)
            {
                await _unitOfWork.CampaignApprovalLogs.AddAsync(new CampaignApprovalLog
                {
                    CampaignId = created.CampaignId,
                    Action = "Approved",
                    ActorId = dto.CreatedByAccountId,
                    Note = "Auto-approved: created by Admin",
                    CreatedAt = _timeProvider.UtcNow
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Campaign {CampaignId} created (status={Status}) by Account {AccountId}",
                created.CampaignId, created.Status, dto.CreatedByAccountId);

            var result = _mapper.Map<CampaignDto>(created);
            return Result<CampaignDto>.Success(result);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }


    public async Task<Result<CampaignDto>> UpdateCampaignAsync(
        UpdateCampaignDto dto,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<CampaignDto>.ValidationFailure(errors);
        }

        var upErr = await _rules.ValidateUpdateDtoAsync(dto, cancellationToken);
        if (upErr is not null)
            return Result<CampaignDto>.Failure(upErr.ErrorCode!, upErr.ErrorMessage!);

        var existing = await _unitOfWork.Campaigns.GetByIdAsync(dto.CampaignId, cancellationToken);
        if (existing is null)
            return Result<CampaignDto>.NotFound("Campaign", dto.CampaignId);

        if (ShouldHideDraftCampaignFromCurrentAdminViewer(existing))
            return Result<CampaignDto>.NotFound("Campaign", dto.CampaignId);

        if (!EditableStatuses.Contains(existing.Status))
            return Result<CampaignDto>.Failure(
                ToyStore.Application.Campaigns.CampaignErrorCodes.InvalidStatusTransition,
                $"Campaign cannot be edited in status '{existing.Status}'. Only Draft and Rejected campaigns can be modified.");

        var ownershipCheck = _rules.ValidateActorCanModifyCampaign(
            existing,
            _currentUser.AccountId,
            _currentUser.RoleName == "Admin");
        if (ownershipCheck.IsFailure)
            return Result<CampaignDto>.Failure(ownershipCheck.ErrorCode!, ownershipCheck.ErrorMessage!);

        var isDuplicate = await _unitOfWork.Campaigns.ExistsByNameAsync(
            dto.CampaignName.Trim(), dto.CampaignId, cancellationToken);
        if (isDuplicate)
            return Result<CampaignDto>.Conflict("Campaign name already exists.");

        if (existing.Status == "Rejected")
        {
            existing.Status = "Draft";
            existing.ReviewNote = null;
            existing.ReviewedByAccountId = null;
            existing.ReviewedAt = null;
            existing.ApprovedExpireAt = null;
        }

        existing.CampaignName = dto.CampaignName.Trim();
        existing.TemplateCode = string.IsNullOrWhiteSpace(dto.TemplateCode) ? null : dto.TemplateCode.Trim();
        existing.ReferenceType = string.IsNullOrWhiteSpace(dto.ReferenceType) ? null : dto.ReferenceType.Trim().ToUpper();
        existing.ReferenceId = dto.ReferenceId;
        existing.TitleOverride = string.IsNullOrWhiteSpace(dto.TitleOverride) ? null : dto.TitleOverride.Trim();
        existing.MessageOverride = string.IsNullOrWhiteSpace(dto.MessageOverride) ? null : dto.MessageOverride.Trim();
        existing.TargetType = dto.TargetType;
        existing.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        existing.ActionType = string.IsNullOrWhiteSpace(dto.ActionType) ? null : dto.ActionType.Trim();
        existing.ActionTarget = string.IsNullOrWhiteSpace(dto.ActionTarget) ? null : dto.ActionTarget.Trim();

        // Always derive ActionType/ActionTarget from resolver — don't trust dto values.
        await ApplyActionTargetFromResolverAsync(existing, cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var updated = await _unitOfWork.Campaigns.UpdateAsync(existing, dto.Targets, _timeProvider.UtcNow, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Campaign {CampaignId} updated (status={Status})",
                updated.CampaignId, updated.Status);

            var result = _mapper.Map<CampaignDto>(updated);
            return Result<CampaignDto>.Success(result);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }


    public async Task<Result> CancelCampaignAsync(
        int campaignId,
        int actorAccountId,
        bool actorIsAdmin,
        CancellationToken cancellationToken = default)
    {
        if (campaignId <= 0)
            return Result.Failure("VALIDATION_ERROR", "Campaign ID must be greater than 0.");

        var campaign = await _unitOfWork.Campaigns.GetForUpdateAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result.NotFound("Campaign", campaignId);

        var v = await _rules.ValidateCancelAsync(campaign, actorAccountId, actorIsAdmin, cancellationToken);
        if (v.IsFailure) return v;

        var now = _timeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Campaigns.MarkLiveReferenceSnapshotsStaleAsync(
                campaignId, "Campaign cancelled", now, cancellationToken);

            campaign.Status = "Cancelled";
            campaign.UpdatedAt = now;
            _unitOfWork.Campaigns.Update(campaign);

            if (campaign.CampaignSchedule is not null)
            {
                campaign.CampaignSchedule.ExecutionStatus = "Cancelled";
                campaign.CampaignSchedule.UpdatedAt = now;
            }

            await _unitOfWork.CampaignApprovalLogs.AddAsync(new CampaignApprovalLog
            {
                CampaignId = campaignId,
                Action = "Cancelled",
                ActorId = actorAccountId,
                Note = null,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation("Campaign {CampaignId} cancelled by {Actor}", campaignId, actorAccountId);
        return Result.Success();
    }

    public async Task<Result> SubmitCampaignForReviewAsync(
        int campaignId,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        if (campaignId <= 0 || accountId <= 0)
            return Result.Failure("VALIDATION_ERROR", "Invalid campaign or account id.");

        var campaign = await _unitOfWork.Campaigns.GetForUpdateAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result.NotFound("Campaign", campaignId);

        var v = await _rules.ValidateSubmitAsync(campaign, accountId, cancellationToken);
        if (v.IsFailure) return v;

        var now = _timeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            campaign.Status = "PendingApproval";
            campaign.SubmittedByAccountId = accountId;
            campaign.SubmittedAt = now;
            campaign.UpdatedAt = now;
            _unitOfWork.Campaigns.Update(campaign);

            await _unitOfWork.CampaignApprovalLogs.AddAsync(new CampaignApprovalLog
            {
                CampaignId = campaignId,
                Action = "Submitted",
                ActorId = accountId,
                Note = null,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation("Campaign {CampaignId} submitted for review by {AccountId}", campaignId, accountId);
        return Result.Success();
    }


    public async Task<Result> ReviewCampaignAsync(
        int campaignId,
        ReviewCampaignDto dto,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        if (campaignId <= 0 || accountId <= 0)
            return Result.Failure("VALIDATION_ERROR", "Invalid campaign or account id.");

        var validation = await _reviewValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result.ValidationFailure(errors);
        }

        if (dto.Action is not ("Approved" or "Rejected"))
            return Result.Failure("VALIDATION_ERROR", "Action must be Approved or Rejected.");

        var campaign = await _unitOfWork.Campaigns.GetForUpdateAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result.NotFound("Campaign", campaignId);

        var reviewerIsAdmin = string.Equals(_currentUser.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);

        if (dto.Action == "Rejected")
        {
            var rv = await _rules.ValidateRejectAsync(campaign, accountId, dto.ReviewNote, reviewerIsAdmin, cancellationToken);
            if (rv.IsFailure) return rv;
        }
        else
        {
            var av = await _rules.ValidateApproveAsync(campaign, accountId, reviewerIsAdmin, cancellationToken);
            if (av.IsFailure) return av;
        }

        var now = _timeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            campaign.Status = dto.Action;
            campaign.ReviewedByAccountId = accountId;
            campaign.ReviewedAt = now;
            campaign.ReviewNote = string.IsNullOrWhiteSpace(dto.ReviewNote) ? null : dto.ReviewNote.Trim();
            campaign.UpdatedAt = now;

            if (dto.Action == "Approved")
                campaign.ApprovedExpireAt = now.AddDays(7);

            _unitOfWork.Campaigns.Update(campaign);

            await _unitOfWork.CampaignApprovalLogs.AddAsync(new CampaignApprovalLog
            {
                CampaignId = campaignId,
                Action = dto.Action,
                ActorId = accountId,
                Note = campaign.ReviewNote,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        try
        {
            var notifyId = campaign.CreatedByAccountId ?? campaign.SubmittedByAccountId;
            if (notifyId is int nid && nid > 0)
            {
                var (title, message) = dto.Action == "Approved"
                    ? ("Campaign approved",
                        $"Your campaign '{campaign.CampaignName}' was approved. Please schedule the send time before the deadline.")
                    : ("Campaign rejected",
                        $"Your campaign '{campaign.CampaignName}' was rejected. Reason: {campaign.ReviewNote}");

                await _notificationDispatcher.DispatchAsync(new NotificationContext
                {
                    RecipientAccountId = nid,
                    RecipientType = RecipientTypes.Staff,
                    NotificationType = NotificationTypes.System,
                    Title = title,
                    Message = message,
                    SendBell = true,
                    SendEmail = false,
                    IdempotencyKey = $"campaign-review:{campaignId}:{nid}:{dto.Action}:{now.Ticks}"
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Campaign review notification failed for {CampaignId}", campaignId);
        }

        _logger.LogInformation("Campaign {CampaignId} {Action} by {AccountId}", campaignId, dto.Action, accountId);
        return Result.Success();
    }

    public async Task<Result> RecallCampaignAsync(
        int campaignId,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        if (campaignId <= 0 || accountId <= 0)
            return Result.Failure("VALIDATION_ERROR", "Invalid campaign or account id.");

        var campaign = await _unitOfWork.Campaigns.GetForUpdateAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result.NotFound("Campaign", campaignId);

        var v = await _rules.ValidateRecallAsync(campaign, accountId, cancellationToken);
        if (v.IsFailure) return v;

        var now = _timeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            campaign.Status = "Draft";
            campaign.SubmittedByAccountId = null;
            campaign.SubmittedAt = null;
            campaign.UpdatedAt = now;
            _unitOfWork.Campaigns.Update(campaign);

            await _unitOfWork.CampaignApprovalLogs.AddAsync(new CampaignApprovalLog
            {
                CampaignId = campaignId,
                Action = "Recalled",
                ActorId = accountId,
                Note = null,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation("Campaign {CampaignId} recalled by {AccountId}", campaignId, accountId);
        return Result.Success();
    }


    public async Task<Result<ScheduleCampaignResultDto>> ScheduleCampaignAsync(
        int campaignId,
        ScheduleCampaignDto dto,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        if (campaignId <= 0 || accountId <= 0)
            return Result<ScheduleCampaignResultDto>.Failure("VALIDATION_ERROR", "Invalid campaign or account id.");

        var validation = await _scheduleValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<ScheduleCampaignResultDto>.ValidationFailure(errors);
        }

        if (!dto.ScheduledAt.HasValue)
            return Result<ScheduleCampaignResultDto>.Failure(
                ToyStore.Application.Campaigns.CampaignErrorCodes.ScheduledAtRequired,
                "Scheduled time is required.");

        var scheduledAt = dto.ScheduledAt.Value;
        var warnings = new List<string>();

        var campaign = await _unitOfWork.Campaigns.GetForUpdateAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result<ScheduleCampaignResultDto>.NotFound("Campaign", campaignId);

        if (campaign.CampaignSchedule is not null)
            return Result<ScheduleCampaignResultDto>.Conflict(
                "Campaign already has a schedule. Use reschedule to change the send time.");

        var scheduleOwnershipCheck = _rules.ValidateActorCanModifyCampaign(
            campaign,
            accountId,
            _currentUser.RoleName == "Admin");
        if (scheduleOwnershipCheck.IsFailure)
            return Result<ScheduleCampaignResultDto>.Failure(scheduleOwnershipCheck.ErrorCode!, scheduleOwnershipCheck.ErrorMessage!);

        var sv = await _rules.ValidateScheduleAsync(
            campaign, scheduledAt, dto.ValidFrom, dto.ValidTo, warnings, cancellationToken);
        if (sv.IsFailure)
            return Result<ScheduleCampaignResultDto>.Failure(sv.ErrorCode!, sv.ErrorMessage!);

        var now = _timeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Build snapshot before setting Scheduled — fail fast if reference is missing/unresolvable
            var snap = await _rules.BuildLiveReferenceSnapshotAsync(campaign, cancellationToken);
            if (snap is null && !string.IsNullOrWhiteSpace(campaign.ReferenceType))
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<ScheduleCampaignResultDto>.Failure(
                    ToyStore.Application.Campaigns.CampaignErrorCodes.ReferenceNotFound,
                    "Cannot build reference snapshot — the linked reference may be deleted or inactive.");
            }

            campaign.Status = "Scheduled";
            campaign.ScheduledAt = scheduledAt;
            campaign.ValidFrom = dto.ValidFrom;
            campaign.ValidTo = dto.ValidTo;
            campaign.RescheduleCount = 0;
            campaign.UpdatedAt = now;
            _unitOfWork.Campaigns.Update(campaign);

            await _unitOfWork.CampaignSchedules.AddAsync(new CampaignSchedule
            {
                CampaignId = campaignId,
                ScheduledBy = accountId,
                ScheduledAt = scheduledAt,
                ExecutionStatus = "Waiting",
                AttemptCount = 0,
                MaxAttemptCount = 3,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (snap is not null)
            {
                _unitOfWork.Campaigns.AddReferenceSnapshot(snap);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.Campaigns.AddCampaignScheduleLogAsync(new CampaignScheduleLog
            {
                CampaignId = campaignId,
                ActorId = accountId,
                Action = "Scheduled",
                PreviousScheduledAt = null,
                NewScheduledAt = scheduledAt,
                Reason = null,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.CampaignApprovalLogs.AddAsync(new CampaignApprovalLog
            {
                CampaignId = campaignId,
                Action = "Scheduled",
                ActorId = accountId,
                Note = $"Scheduled at {scheduledAt:O}",
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation("Campaign {CampaignId} scheduled at {ScheduledAt}", campaignId, scheduledAt);
        return Result<ScheduleCampaignResultDto>.Success(new ScheduleCampaignResultDto
        {
            WarningCodes = warnings.Count > 0 ? warnings : null
        });
    }

    public async Task<Result<ScheduleCampaignResultDto>> RescheduleCampaignAsync(
        int campaignId,
        RescheduleCampaignDto dto,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        if (campaignId <= 0 || accountId <= 0)
            return Result<ScheduleCampaignResultDto>.Failure("VALIDATION_ERROR", "Invalid campaign or account id.");

        var val = await _rescheduleValidator.ValidateAsync(dto, cancellationToken);
        if (!val.IsValid)
        {
            var errors = val.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<ScheduleCampaignResultDto>.ValidationFailure(errors);
        }

        var warnings = new List<string>();
        var campaign = await _unitOfWork.Campaigns.GetForUpdateAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result<ScheduleCampaignResultDto>.NotFound("Campaign", campaignId);

        var rescheduleOwnershipCheck = _rules.ValidateActorCanModifyCampaign(
            campaign,
            accountId,
            _currentUser.RoleName == "Admin");
        if (rescheduleOwnershipCheck.IsFailure)
            return Result<ScheduleCampaignResultDto>.Failure(rescheduleOwnershipCheck.ErrorCode!, rescheduleOwnershipCheck.ErrorMessage!);

        var oldAt = campaign.CampaignSchedule?.ScheduledAt;
        var rv = await _rules.ValidateRescheduleAsync(campaign, dto.NewScheduledAt, dto.Reason, warnings, cancellationToken);
        if (rv.IsFailure)
            return Result<ScheduleCampaignResultDto>.Failure(rv.ErrorCode!, rv.ErrorMessage!);

        var now = _timeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Build new snapshot before marking old one stale — fail if reference missing
            var snap = await _rules.BuildLiveReferenceSnapshotAsync(campaign, cancellationToken);
            if (snap is null && !string.IsNullOrWhiteSpace(campaign.ReferenceType))
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<ScheduleCampaignResultDto>.Failure(
                    ToyStore.Application.Campaigns.CampaignErrorCodes.ReferenceNotFound,
                    "Cannot build reference snapshot for reschedule — the linked reference may be deleted or inactive.");
            }

            await _unitOfWork.Campaigns.MarkLiveReferenceSnapshotsStaleAsync(
                campaignId, "Rescheduled", now, cancellationToken);

            campaign.ScheduledAt = dto.NewScheduledAt;
            campaign.RescheduleCount++;
            campaign.UpdatedAt = now;
            _unitOfWork.Campaigns.Update(campaign);

            if (campaign.CampaignSchedule is null)
                throw new InvalidOperationException("CampaignSchedule missing for reschedule.");

            campaign.CampaignSchedule.ScheduledAt = dto.NewScheduledAt;
            campaign.CampaignSchedule.ExecutionStatus = "Waiting";
            campaign.CampaignSchedule.AttemptCount = 0;
            campaign.CampaignSchedule.LockedByJobId = null;
            campaign.CampaignSchedule.LockedAt = null;
            campaign.CampaignSchedule.UpdatedAt = now;

            if (snap is not null)
            {
                _unitOfWork.Campaigns.AddReferenceSnapshot(snap);
            }

            await _unitOfWork.Campaigns.AddCampaignScheduleLogAsync(new CampaignScheduleLog
            {
                CampaignId = campaignId,
                ActorId = accountId,
                Action = "Rescheduled",
                PreviousScheduledAt = oldAt,
                NewScheduledAt = dto.NewScheduledAt,
                Reason = dto.Reason,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.CampaignApprovalLogs.AddAsync(new CampaignApprovalLog
            {
                CampaignId = campaignId,
                Action = "Rescheduled",
                ActorId = accountId,
                Note = dto.Reason,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation("Campaign {CampaignId} rescheduled to {At}", campaignId, dto.NewScheduledAt);
        return Result<ScheduleCampaignResultDto>.Success(new ScheduleCampaignResultDto
        {
            WarningCodes = warnings.Count > 0 ? warnings : null
        });
    }

    /// <summary>
    /// Any viewer (Admin or Staff) must not see or open another user's Draft
    /// until it is submitted for review (PendingApproval+).
    /// </summary>
    private bool ShouldHideDraftCampaignFromCurrentAdminViewer(Campaign campaign)
    {
        if (campaign.Status != "Draft")
            return false;
        return campaign.CreatedByAccountId != _currentUser.AccountId;
    }


    public Task<Result<List<ReferenceTypeDto>>> GetReferenceTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var list = _resolverFactory.GetAll()
            .Select(r => new ReferenceTypeDto
            {
                ReferenceType = r.ReferenceType,
                DisplayName = r.ReferenceType switch
                {
                    "VOUCHER" => "Voucher discount",
                    "PRODUCT" => "New product",
                    "BLOG" => "Blog post",
                    "SALE" => "Sale promotion",
                    "OTHER" => "Other",
                    _ => r.ReferenceType
                },
                Placeholders = r.AvailablePlaceholders.ToList()
            })
            .ToList();

        if (list.All(x => x.ReferenceType != "OTHER"))
        {
            list.Add(new ReferenceTypeDto
            {
                ReferenceType = "OTHER",
                DisplayName = "Other",
                Placeholders = new List<PlaceholderInfoDto>()
            });
        }

        return Task.FromResult(Result<List<ReferenceTypeDto>>.Success(list));
    }

    /// <summary>
    /// Sets ActionType="ROUTE" and ActionTarget from the resolver's DefaultActionTarget.
    /// Call after the campaign entity has reference fields populated.
    /// Ignores dto.ActionType/ActionTarget — the resolver is the single source of truth.
    /// </summary>
    private async Task ApplyActionTargetFromResolverAsync(Campaign campaign, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(campaign.ReferenceType) || !campaign.ReferenceId.HasValue)
        {
            campaign.ActionType = null;
            campaign.ActionTarget = null;
            return;
        }

        var resolver = _resolverFactory.GetResolver(campaign.ReferenceType.Trim().ToUpperInvariant());
        if (resolver is null) return;

        var resolved = await resolver.ResolveAsync(campaign.ReferenceId.Value, ct);
        if (resolved?.DefaultActionTarget is null) return;

        campaign.ActionType = "ROUTE";
        campaign.ActionTarget = resolved.DefaultActionTarget;
    }

    /// <inheritdoc />
    public async Task<Result> DeleteCampaignAsync(
        int campaignId,
        int actorAccountId,
        bool actorIsAdmin,
        CancellationToken cancellationToken = default)
    {
        if (campaignId <= 0)
            return Result.Failure("VALIDATION_ERROR", "Campaign ID must be greater than 0.");

        // Fetch campaign (bao gom ca IsDeleted = true de biet no da bi xoa chua)
        var campaign = await _unitOfWork.Campaigns.GetByIdAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result.NotFound("Campaign", campaignId);

        // Chi cho phep xoa khi campaign da hoan thanh (Sent), bi huy (Cancelled) hoac that bai (Failed)
        var deletableStatuses = new[] { "Sent", "Cancelled", "Failed" };
        if (!deletableStatuses.Contains(campaign.Status))
            return Result.Failure(
                ToyStore.Application.Campaigns.CampaignErrorCodes.InvalidStatusTransition,
                $"Campaign cannot be deleted in status '{campaign.Status}'. Only Sent, Cancelled, or Failed campaigns can be deleted.");

        // Staff chi duoc xoa campaign do chinh ho tao
        if (!actorIsAdmin && campaign.CreatedByAccountId != actorAccountId)
            return Result.Failure("FORBIDDEN", "You are not allowed to delete this campaign.");

        var deleted = await _unitOfWork.Campaigns.SoftDeleteAsync(campaignId, _timeProvider.UtcNow, cancellationToken);
        if (!deleted)
            return Result.Failure("BUSINESS_RULE_VIOLATION", "Campaign could not be deleted. It may have already been deleted.");

        _logger.LogInformation(
            "Campaign {CampaignId} soft-deleted by Account {ActorId} (isAdmin={IsAdmin})",
            campaignId, actorAccountId, actorIsAdmin);

        return Result.Success();
    }
}
