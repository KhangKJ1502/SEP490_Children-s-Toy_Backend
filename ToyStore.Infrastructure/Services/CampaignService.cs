using Microsoft.Extensions.Logging;
using AutoMapper;
using FluentValidation;
using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators.Campaigns;

namespace ToyStore.Infrastructure.Services;

public class CampaignService : ICampaignService
{
    private static readonly HashSet<string> ValidStatuses = ["Draft", "Scheduled", "Sending", "Sent", "Cancelled"];
    private static readonly HashSet<string> ValidSourceTypes = ["ADMIN", "SYSTEM"];
    private static readonly HashSet<string> ValidSortFields = ["createdat", "name", "status"];

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CampaignService> _logger;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateCampaignDto> _createCampaignValidator;

    public CampaignService(
        IUnitOfWork unitOfWork,
        ILogger<CampaignService> logger,
        IMapper mapper,
        IValidator<CreateCampaignDto> createCampaignValidator)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        _createCampaignValidator = createCampaignValidator;
    }

    public async Task<Result<PaginatedResponse<CampaignListDto>>> GetCampaignsAsync(
         CampaignQueryDto query,
         CancellationToken cancellationToken = default)
    {
        if (query.PageNumber < 1)
        {
            return Result<PaginatedResponse<CampaignListDto>>.Failure(
                "VALIDATION_ERROR", "Page number must be greater than 0.");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            return Result<PaginatedResponse<CampaignListDto>>.Failure(
                "VALIDATION_ERROR", "Page size must be between 1 and 100.");
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            !ValidStatuses.Contains(query.Status))
        {
            return Result<PaginatedResponse<CampaignListDto>>.Failure(
                "VALIDATION_ERROR",
                $"Invalid status '{query.Status}'. Allowed values: {string.Join(", ", ValidStatuses)}.");
        }

        if (!string.IsNullOrWhiteSpace(query.SourceType) &&
            !ValidSourceTypes.Contains(query.SourceType))
        {
            return Result<PaginatedResponse<CampaignListDto>>.Failure(
                "VALIDATION_ERROR",
                $"Invalid sourceType '{query.SourceType}'. Allowed values: {string.Join(", ", ValidSourceTypes)}.");
        }

        if (!string.IsNullOrWhiteSpace(query.SortBy) &&
            !ValidSortFields.Contains(query.SortBy.Trim().ToLowerInvariant()))
        {
            return Result<PaginatedResponse<CampaignListDto>>.Failure(
                "VALIDATION_ERROR",
                $"Invalid sortBy '{query.SortBy}'. Allowed values: createdAt, name, status.");
        }

        if (query.StartDate.HasValue && query.EndDate.HasValue &&
            query.StartDate.Value.Date > query.EndDate.Value.Date)
        {
            return Result<PaginatedResponse<CampaignListDto>>.Failure(
                "VALIDATION_ERROR", "StartDate must be less than or equal to EndDate.");
        }

        var items = await _unitOfWork.Campaigns.GetPagedAsync(query, cancellationToken);
        var totalCount = await _unitOfWork.Campaigns.CountAsync(query, cancellationToken);

        var mappedItems = _mapper.Map<List<CampaignListDto>>(items);
        var response = new PaginatedResponse<CampaignListDto>(mappedItems, totalCount, query.PageNumber, query.PageSize);

        _logger.LogDebug(
            "GetCampaignsAsync returned {Count}/{Total} campaigns (page {Page}, size {Size})",
            items.Count, totalCount, query.PageNumber, query.PageSize);

        return Result<PaginatedResponse<CampaignListDto>>.Success(response);
    }

    public async Task<Result<CampaignDto>> GetCampaignByIdAsync(
        int campaignId,
        CancellationToken cancellationToken = default)
    {
        if (campaignId <= 0)
        {
            return Result<CampaignDto>.Failure("VALIDATION_ERROR", "Campaign ID must be greater than 0.");
        }

        var campaign = await _unitOfWork.Campaigns.GetByIdAsync(campaignId, cancellationToken);

        if (campaign is null)
        {
            return Result<CampaignDto>.NotFound("Campaign", campaignId);
        }

        _logger.LogDebug("GetCampaignByIdAsync returned campaign {CampaignId}", campaignId);

        var mappedCampaign = _mapper.Map<CampaignDto>(campaign);
        return Result<CampaignDto>.Success(mappedCampaign);
    }

    public async Task<Result<CampaignDto>> CreateCampaignAsync(
        CreateCampaignDto dto,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _createCampaignValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<CampaignDto>.ValidationFailure(errors);
        }

        var isDuplicate = await _unitOfWork.Campaigns.ExistsByNameAsync(dto.CampaignName.Trim(), cancellationToken);
        if (isDuplicate)
        {
            return Result<CampaignDto>.Conflict("Campaign name already exists.");
        }

        if (!string.IsNullOrWhiteSpace(dto.TemplateCode))
        {
            var templateExists = await _unitOfWork.Templates.ExistsByCodeAsync(dto.TemplateCode.Trim(), cancellationToken);
            if (!templateExists)
            {
                return Result<CampaignDto>.NotFound("Template", dto.TemplateCode);
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.Campaigns.CreateAsync(dto, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Campaign {CampaignId} created successfully by Account {AccountId}",
                created.CampaignId, dto.CreatedByAccountId);

            var mappedCreated = _mapper.Map<CampaignDto>(created);
            return Result<CampaignDto>.Success(mappedCreated);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

