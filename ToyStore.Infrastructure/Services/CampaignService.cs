using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Services.Resolvers;

namespace ToyStore.Infrastructure.Services;

public class CampaignService : ICampaignService
{
    private static readonly HashSet<string> ValidStatuses =
        ["Draft", "Scheduled", "Sending", "Sent", "Cancelled", "Failed"];

    private static readonly HashSet<string> ValidSourceTypes = ["ADMIN", "SYSTEM"];
    private static readonly HashSet<string> ValidSortFields = ["createdat", "name", "status"];
    private static readonly HashSet<string> EditableStatuses = ["Draft", "Scheduled"];
    private static readonly HashSet<string> ValidReferenceTypes = ["VOUCHER", "PRODUCT", "BLOG", "SALE", "OTHER"];

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CampaignService> _logger;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateCampaignDto> _createValidator;
    private readonly IValidator<UpdateCampaignDto> _updateValidator;
    private readonly BusinessObjectResolverFactory _resolverFactory;
    private readonly ITimeProvider _timeProvider;

    public CampaignService(
        IUnitOfWork unitOfWork,
        ILogger<CampaignService> logger,
        IMapper mapper,
        IValidator<CreateCampaignDto> createValidator,
        IValidator<UpdateCampaignDto> updateValidator,
        BusinessObjectResolverFactory resolverFactory,
        ITimeProvider timeProvider)
    {
        _unitOfWork      = unitOfWork;
        _logger          = logger;
        _mapper          = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _resolverFactory = resolverFactory;
        _timeProvider    = timeProvider;
    }

    // ── GET list ──────────────────────────────────────────────────────────────

    public async Task<Result<PaginatedResponse<CampaignListDto>>> GetCampaignsAsync(
        CampaignQueryDto query,
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

        var items = await _unitOfWork.Campaigns.GetPagedAsync(query, cancellationToken);
        var totalCount = await _unitOfWork.Campaigns.CountAsync(query, cancellationToken);
        var mapped = _mapper.Map<List<CampaignListDto>>(items);
        var response = new PaginatedResponse<CampaignListDto>(mapped, totalCount, query.PageNumber, query.PageSize);

        return Result<PaginatedResponse<CampaignListDto>>.Success(response);
    }

    // ── GET by ID ─────────────────────────────────────────────────────────────

    public async Task<Result<CampaignDto>> GetCampaignByIdAsync(
        int campaignId,
        CancellationToken cancellationToken = default)
    {
        if (campaignId <= 0)
            return Result<CampaignDto>.Failure("VALIDATION_ERROR", "Campaign ID must be greater than 0.");

        var campaign = await _unitOfWork.Campaigns.GetByIdAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result<CampaignDto>.NotFound("Campaign", campaignId);

        var dto = _mapper.Map<CampaignDto>(campaign);

        // Enrich with resolved reference data
        if (!string.IsNullOrWhiteSpace(campaign.ReferenceType) && campaign.ReferenceId.HasValue)
        {
            dto.ResolvedReference = await _resolverFactory.ResolveAsync(
                campaign.ReferenceType, campaign.ReferenceId.Value, cancellationToken);
        }

       var tmpl = campaign.TemplateCodeNavigation;
        dto.ResolvedTitle   = !string.IsNullOrWhiteSpace(campaign.TitleOverride)
            ? campaign.TitleOverride
            : tmpl?.TitleTemplate;
        dto.ResolvedMessage = !string.IsNullOrWhiteSpace(campaign.MessageOverride)
            ? campaign.MessageOverride
            : tmpl?.MessageTemplate;

        return Result<CampaignDto>.Success(dto);
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

        var totalCount = await _unitOfWork.Deliveries.CountByCampaignAsync(campaignId, status, cancellationToken);
        var items = await _unitOfWork.Deliveries.GetByCampaignPagedAsync(campaignId, pageNumber, pageSize, status, cancellationToken);

        var deliveryDtos = items.Select(d => new CampaignDeliveryDto
        {
            DeliveryId  = d.DeliveryId,
            AccountId   = d.AccountId,
            AccountName = d.Account.AccountName,
            Email       = d.Account.Email,
            Status      = d.Status,
            Title       = d.Title,
            Message     = d.Message,
            ReadAt      = d.ReadAt,
            CreatedAt   = d.CreatedAt
        }).ToList();

        var response = new PaginatedResponse<CampaignDeliveryDto>(deliveryDtos, totalCount, pageNumber, pageSize);
        return Result<PaginatedResponse<CampaignDeliveryDto>>.Success(response);
    }

    // ── CREATE ────────────────────────────────────────────────────────────────

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

        var isDuplicate = await _unitOfWork.Campaigns.ExistsByNameAsync(
            dto.CampaignName.Trim(), cancellationToken: cancellationToken);
        if (isDuplicate)
            return Result<CampaignDto>.Conflict("Campaign name already exists.");

        if (!string.IsNullOrWhiteSpace(dto.TemplateCode))
        {
            var templateExists = await _unitOfWork.Templates.ExistsByCodeAsync(dto.TemplateCode.Trim(), cancellationToken);
            if (!templateExists)
                return Result<CampaignDto>.NotFound("Template", dto.TemplateCode);
        }

        if (!string.IsNullOrWhiteSpace(dto.ReferenceType))
        {
            var refCheck = await ValidateReferenceAsync(dto.ReferenceType, dto.ReferenceId, cancellationToken);
            if (refCheck is not null) return refCheck;
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.Campaigns.CreateAsync(dto, cancellationToken);
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

    // ── UPDATE ────────────────────────────────────────────────────────────────

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

        var existing = await _unitOfWork.Campaigns.GetByIdAsync(dto.CampaignId, cancellationToken);
        if (existing is null)
            return Result<CampaignDto>.NotFound("Campaign", dto.CampaignId);

        if (!EditableStatuses.Contains(existing.Status))
            return Result<CampaignDto>.BusinessError(
                $"Campaign cannot be edited in status '{existing.Status}'. Only Draft and Scheduled campaigns can be modified.");

        var isDuplicate = await _unitOfWork.Campaigns.ExistsByNameAsync(
            dto.CampaignName.Trim(), dto.CampaignId, cancellationToken);
        if (isDuplicate)
            return Result<CampaignDto>.Conflict("Campaign name already exists.");

        if (!string.IsNullOrWhiteSpace(dto.TemplateCode))
        {
            var templateExists = await _unitOfWork.Templates.ExistsByCodeAsync(dto.TemplateCode.Trim(), cancellationToken);
            if (!templateExists)
                return Result<CampaignDto>.NotFound("Template", dto.TemplateCode);
        }

        if (!string.IsNullOrWhiteSpace(dto.ReferenceType))
        {
            var refCheck = await ValidateReferenceAsync(dto.ReferenceType, dto.ReferenceId, cancellationToken);
            if (refCheck is not null) return refCheck;
        }

        // Apply fields
        existing.CampaignName = dto.CampaignName.Trim();
        // Neu khong co ScheduledAt, gui ngay lap tuc: dat ScheduledAt = Now de Worker xu ly
        existing.ScheduledAt = dto.ScheduledAt ?? _timeProvider.UtcNow;

        existing.TemplateCode = string.IsNullOrWhiteSpace(dto.TemplateCode) ? null : dto.TemplateCode.Trim();
        existing.ReferenceType = string.IsNullOrWhiteSpace(dto.ReferenceType) ? null : dto.ReferenceType.Trim().ToUpper();
        existing.ReferenceId = dto.ReferenceId;
        existing.TitleOverride = string.IsNullOrWhiteSpace(dto.TitleOverride) ? null : dto.TitleOverride.Trim();
        existing.MessageOverride = string.IsNullOrWhiteSpace(dto.MessageOverride) ? null : dto.MessageOverride.Trim();
        existing.TargetType = dto.TargetType;
        existing.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        existing.ActionType = string.IsNullOrWhiteSpace(dto.ActionType) ? null : dto.ActionType.Trim();
        existing.ActionTarget = string.IsNullOrWhiteSpace(dto.ActionTarget) ? null : dto.ActionTarget.Trim();

        // Auto-transition to Scheduled (luon Scheduled de Worker xu ly)
        existing.Status = "Scheduled";

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var updated = await _unitOfWork.Campaigns.UpdateAsync(existing, dto.Targets, cancellationToken);
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

    // ── CANCEL ────────────────────────────────────────────────────────────────

    public async Task<Result> CancelCampaignAsync(
        int campaignId,
        CancellationToken cancellationToken = default)
    {
        if (campaignId <= 0)
            return Result.Failure("VALIDATION_ERROR", "Campaign ID must be greater than 0.");

        var existing = await _unitOfWork.Campaigns.GetByIdAsync(campaignId, cancellationToken);
        if (existing is null)
            return Result.NotFound("Campaign", campaignId);

        if (!EditableStatuses.Contains(existing.Status))
            return Result.BusinessError(
                $"Campaign cannot be cancelled in status '{existing.Status}'. Only Draft and Scheduled campaigns can be cancelled.");

        var cancelled = await _unitOfWork.Campaigns.CancelAsync(campaignId, cancellationToken);
        if (!cancelled)
            return Result.NotFound("Campaign", campaignId);

        _logger.LogInformation("Campaign {CampaignId} cancelled", campaignId);
        return Result.Success();
    }

    // ── REFERENCE TYPES ───────────────────────────────────────────────────────

    public Task<Result<List<ReferenceTypeDto>>> GetReferenceTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var list = _resolverFactory.GetAll()
            .Select(r => new ReferenceTypeDto
            {
                ReferenceType = r.ReferenceType,
                DisplayName = r.ReferenceType switch
                {
                    "VOUCHER" => "Voucher giảm giá",
                    "PRODUCT" => "Sản phẩm mới",
                    "BLOG" => "Bài blog",
                    "SALE" => "Chương trình sale",
                    "OTHER" => "Khác",
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
                DisplayName = "Khác",
                Placeholders = new List<PlaceholderInfoDto>()
            });
        }

        return Task.FromResult(Result<List<ReferenceTypeDto>>.Success(list));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Kiem tra ReferenceType hop le va ReferenceId co ton tai khong.
    /// Tra ve Result loi neu co van de, null neu OK.
    /// </summary>
    private async Task<Result<CampaignDto>?> ValidateReferenceAsync(
        string referenceType,
        int? referenceId,
        CancellationToken cancellationToken)
    {
        var upper = referenceType.Trim().ToUpper();
        if (!ValidReferenceTypes.Contains(upper))
            return Result<CampaignDto>.Failure(
                "VALIDATION_ERROR",
                $"Invalid referenceType '{referenceType}'. Allowed: {string.Join(", ", ValidReferenceTypes)}.");

        if (!referenceId.HasValue || referenceId.Value <= 0)
            return Result<CampaignDto>.Failure(
                "VALIDATION_ERROR",
                "ReferenceId is required and must be greater than 0 when ReferenceType is set.");

        if (upper == "OTHER")
            return null;

        var resolved = await _resolverFactory.ResolveAsync(upper, referenceId.Value, cancellationToken);
        if (resolved is null)
            return Result<CampaignDto>.NotFound(upper, referenceId.Value);

        return null;
    }
}
