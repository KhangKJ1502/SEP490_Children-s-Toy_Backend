using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
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
    private readonly CreateTemplateValidator _createTemplateValidator;
    private readonly UpdateTemplateValidator _updateTemplateValidator;

    public TemplateService(IUnitOfWork unitOfWork, ILogger<TemplateService> logger, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        _createTemplateValidator = new CreateTemplateValidator();
        _updateTemplateValidator = new UpdateTemplateValidator();
    }

    public async Task<Result<PaginatedResponse<TemplateListDto>>> GetTemplatesAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
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

        var items = await _unitOfWork.Templates.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            cancellationToken);

        var totalCount = await _unitOfWork.Templates.CountAsync(searchTerm, cancellationToken);
        var mappedItems = _mapper.Map<List<TemplateListDto>>(items);

        var response = new PaginatedResponse<TemplateListDto>(mappedItems, totalCount, pageNumber, pageSize);
        return Result<PaginatedResponse<TemplateListDto>>.Success(response);
    }

    public async Task<Result<TemplateListDto>> CreateTemplateAsync(
        CreateTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _createTemplateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<TemplateListDto>.ValidationFailure(errors);
        }

        var normalizedCode = dto.TemplateCode.Trim();
        var normalizedTitle = dto.TitleTemplate.Trim();
        var normalizedMessage = dto.MessageTemplate.Trim();

        var isDuplicate = await _unitOfWork.Templates.ExistsByCodeAsync(normalizedCode, cancellationToken);
        if (isDuplicate)
        {
            return Result<TemplateListDto>.Conflict("Template code already exists.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.Templates.CreateAsync(
                normalizedCode,
                normalizedTitle,
                normalizedMessage,
                dto.IsActive,
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Template {TemplateId} created successfully.", created.TemplateId);

            return Result<TemplateListDto>.Success(_mapper.Map<TemplateListDto>(created));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create template with code {TemplateCode}", dto.TemplateCode);
            throw;
        }
    }

    public Task<Result<PaginatedResponse<TemplateListDto>>> SearchTemplatesAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Task.FromResult(
                Result<PaginatedResponse<TemplateListDto>>.Failure(
                    "VALIDATION_ERROR",
                    "Search term is required."));
        }

        return GetTemplatesAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm.Trim(),
            cancellationToken);
    }

    public async Task<Result<TemplateListDto>> UpdateTemplateAsync(
        short templateId,
        UpdateTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (templateId <= 0)
        {
            return Result<TemplateListDto>.Failure("VALIDATION_ERROR", "Template ID must be greater than 0.");
        }

        var validationResult = await _updateTemplateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<TemplateListDto>.ValidationFailure(errors);
        }

        var existing = await _unitOfWork.Templates.GetByIdAsync(templateId, cancellationToken);
        if (existing == null)
        {
            return Result<TemplateListDto>.NotFound("Template", templateId);
        }

        var isUsed = await _unitOfWork.Templates.IsUsedAsync(existing.TemplateCode, cancellationToken);
        if (isUsed)
        {
            return Result<TemplateListDto>.Failure(
                "BUSINESS_RULE_VIOLATION",
                "Cannot edit template because it is already in use by campaigns or deliveries.");
        }

        var normalizedCode = dto.TemplateCode.Trim();
        var normalizedTitle = dto.TitleTemplate.Trim();
        var normalizedMessage = dto.MessageTemplate.Trim();

        var isDuplicate = await _unitOfWork.Templates.ExistsByCodeExceptIdAsync(
            normalizedCode,
            templateId,
            cancellationToken);
        if (isDuplicate)
        {
            return Result<TemplateListDto>.Conflict("Template code already exists.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var updated = await _unitOfWork.Templates.UpdateAsync(
                templateId,
                normalizedCode,
                normalizedTitle,
                normalizedMessage,
                dto.IsActive,
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Template {TemplateId} updated successfully.", updated.TemplateId);

            return Result<TemplateListDto>.Success(_mapper.Map<TemplateListDto>(updated));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update template {TemplateId}", templateId);
            throw;
        }
    }
}