using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Templates;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators.Templates;

namespace ToyStore.Infrastructure.Services;

public class TemplateService : ITemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TemplateService> _logger;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateTemplateDto> _createTemplateValidator;
    private readonly IValidator<UpdateTemplateDto> _updateTemplateValidator;

    public TemplateService(
        IUnitOfWork unitOfWork,
        ILogger<TemplateService> logger,
        IMapper mapper,
        IValidator<CreateTemplateDto> createTemplateValidator,
        IValidator<UpdateTemplateDto> updateTemplateValidator)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        _createTemplateValidator = createTemplateValidator;
        _updateTemplateValidator = updateTemplateValidator;
    }

    public async Task<Result<PaginatedResponse<TemplateListDto>>> GetTemplatesAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        bool? isActive = null,
        string? usageScope = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            return Result<PaginatedResponse<TemplateListDto>>.Failure("VALIDATION_ERROR", "Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<TemplateListDto>>.Failure("VALIDATION_ERROR", "Page size must be between 1 and 100.");
        }

        if (!string.IsNullOrWhiteSpace(usageScope))
        {
            var normalizedScope = usageScope.Trim().ToUpperInvariant();
            if (normalizedScope != "SYSTEM" && normalizedScope != "ADMIN")
            {
                return Result<PaginatedResponse<TemplateListDto>>.Failure("VALIDATION_ERROR", "Usage scope must be SYSTEM or ADMIN.");
            }
            usageScope = normalizedScope;
        }

        var items = await _unitOfWork.Templates.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            isActive,
            usageScope,
            startDate,
            endDate,
            cancellationToken);

        var totalCount = await _unitOfWork.Templates.CountAsync(searchTerm, isActive, usageScope, startDate, endDate, cancellationToken);
        var mappedItems = _mapper.Map<List<TemplateListDto>>(items);
        foreach (var item in mappedItems)
        {
            item.IsUsed = await _unitOfWork.Templates.IsUsedAsync(item.TemplateCode, cancellationToken);
        }

        var response = new PaginatedResponse<TemplateListDto>(mappedItems, totalCount, pageNumber, pageSize);
        return Result<PaginatedResponse<TemplateListDto>>.Success(response);
    }

    public async Task<Result<TemplateListDto>> CreateTemplateAsync(
        CreateTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.TemplateCode != null) dto.TemplateCode = dto.TemplateCode.Trim();
        if (dto.TitleTemplate != null) dto.TitleTemplate = dto.TitleTemplate.Trim();
        if (dto.MessageTemplate != null) dto.MessageTemplate = dto.MessageTemplate.Trim();

        var validationResult = await _createTemplateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<TemplateListDto>.ValidationFailure(errors);
        }

        var isDuplicate = await _unitOfWork.Templates.ExistsByCodeAsync(dto.TemplateCode, cancellationToken);
        if (isDuplicate)
        {
            return Result<TemplateListDto>.Conflict("Template code already exists.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.Templates.CreateAsync(
                dto.TemplateCode,
                dto.TitleTemplate,
                dto.MessageTemplate,
                dto.IsActive,
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Template {TemplateId} created successfully.", created.TemplateId);

            var dtoResult = _mapper.Map<TemplateListDto>(created);
            dtoResult.IsUsed = false;
            return Result<TemplateListDto>.Success(dtoResult);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create template with code {TemplateCode}", dto.TemplateCode);
            throw;
        }
    }

    public async Task<Result<TemplateListDto?>> SaveTemplateAsync(
        short templateId,
        UpdateTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (templateId <= 0)
            return Result<TemplateListDto?>.Failure("VALIDATION_ERROR", "Template ID must be greater than 0.");

        if (!dto.IsDeleted)
        {
            if (dto.TemplateCode != null) dto.TemplateCode = dto.TemplateCode.Trim();
            if (dto.TitleTemplate != null) dto.TitleTemplate = dto.TitleTemplate.Trim();
            if (dto.MessageTemplate != null) dto.MessageTemplate = dto.MessageTemplate.Trim();
        }

        var validationResult = await _updateTemplateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<TemplateListDto?>.ValidationFailure(errors);
        }

        var existing = await _unitOfWork.Templates.GetByIdAsync(templateId, cancellationToken);
        if (existing is null)
            return Result<TemplateListDto?>.NotFound("Template", templateId);

        var isUsed = await _unitOfWork.Templates.IsUsedAsync(existing.TemplateCode, cancellationToken);
        if (isUsed)
        {
            var msg = dto.IsDeleted
                ? "Cannot delete template because it is currently in use by active campaigns or deliveries."
                : "Cannot edit template because it is already in use by campaigns or deliveries.";
            return Result<TemplateListDto?>.Failure("BUSINESS_RULE_VIOLATION", msg);
        }

        if (!dto.IsDeleted)
        {
            var isDuplicate = await _unitOfWork.Templates.ExistsByCodeExceptIdAsync(
                dto.TemplateCode, templateId, cancellationToken);
            if (isDuplicate)
                return Result<TemplateListDto?>.Conflict("Template code already exists.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var saved = await _unitOfWork.Templates.SaveAsync(
                templateId,
                dto.IsDeleted,
                dto.IsDeleted ? null : dto.TemplateCode,
                dto.IsDeleted ? null : dto.TitleTemplate,
                dto.IsDeleted ? null : dto.MessageTemplate,
                dto.IsDeleted ? null : dto.IsActive,
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            if (saved is null)
                return Result<TemplateListDto?>.Failure("BUSINESS_RULE_VIOLATION", "Template could not be saved. It may have already been deleted.");

            if (dto.IsDeleted)
            {
                _logger.LogInformation("Template {TemplateId} (code={TemplateCode}) soft-deleted.", templateId, existing.TemplateCode);
                return Result<TemplateListDto?>.Success(null);
            }

            _logger.LogInformation("Template {TemplateId} updated successfully.", saved.TemplateId);
            var dtoResult = _mapper.Map<TemplateListDto>(saved);
            dtoResult.IsUsed = await _unitOfWork.Templates.IsUsedAsync(saved.TemplateCode, cancellationToken);
            return Result<TemplateListDto?>.Success(dtoResult);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to save template {TemplateId}", templateId);
            throw;
        }
    }
}