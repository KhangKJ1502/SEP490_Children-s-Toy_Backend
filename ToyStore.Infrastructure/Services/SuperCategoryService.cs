using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.SuperCategories;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

public class SuperCategoryService : ISuperCategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SuperCategoryService> _logger;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateSuperCategoryDto> _createSuperCategoryValidator;
    private readonly IValidator<UpdateSuperCategoryDto> _updateSuperCategoryValidator;

    public SuperCategoryService(
        IUnitOfWork unitOfWork,
        ILogger<SuperCategoryService> logger,
        IMapper mapper,
        IValidator<CreateSuperCategoryDto> createSuperCategoryValidator,
        IValidator<UpdateSuperCategoryDto> updateSuperCategoryValidator)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        _createSuperCategoryValidator = createSuperCategoryValidator;
        _updateSuperCategoryValidator = updateSuperCategoryValidator;
    }

    public async Task<Result<PaginatedResponse<SuperCategoryListDto>>> GetSuperCategoriesAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            return Result<PaginatedResponse<SuperCategoryListDto>>.Failure("VALIDATION_ERROR", "Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<SuperCategoryListDto>>.Failure("VALIDATION_ERROR", "Page size must be between 1 and 100.");
        }

        var items = await _unitOfWork.SuperCategories.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            cancellationToken);

        var totalCount = await _unitOfWork.SuperCategories.CountAsync(searchTerm, cancellationToken);

        var mappedItems = _mapper.Map<List<SuperCategoryListDto>>(items);

        var response = new PaginatedResponse<SuperCategoryListDto>(mappedItems, totalCount, pageNumber, pageSize);
        return Result<PaginatedResponse<SuperCategoryListDto>>.Success(response);
    }

    public async Task<Result<SuperCategoryListDto>> GetSuperCategoryByIdAsync(
        short superCategoryId,
        CancellationToken cancellationToken = default)
    {
        var superCategory = await _unitOfWork.SuperCategories.GetByIdAsync(superCategoryId, cancellationToken);
        if (superCategory == null)
        {
            return Result<SuperCategoryListDto>.Failure("NOT_FOUND", $"Super category with ID {superCategoryId} not found.");
        }

        var dto = _mapper.Map<SuperCategoryListDto>(superCategory);
        return Result<SuperCategoryListDto>.Success(dto);
    }

    public async Task<Result<SuperCategoryListDto>> CreateSuperCategoryAsync(
        CreateSuperCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _createSuperCategoryValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<SuperCategoryListDto>.ValidationFailure(errors);
        }

        var normalizedName = dto.SuperCategoryName.Trim();
        var existed = await _unitOfWork.SuperCategories.ExistsByNameAsync(normalizedName, cancellationToken);
        if (existed)
        {
            return Result<SuperCategoryListDto>.Conflict("Super category name already exists.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.SuperCategories.CreateAsync(normalizedName, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Super category {SuperCategoryId} created successfully.", created.SuperCategoryId);

            return Result<SuperCategoryListDto>.Success(_mapper.Map<SuperCategoryListDto>(created));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create super category with name {SuperCategoryName}", dto.SuperCategoryName);
            throw;
        }
    }

    public Task<Result<PaginatedResponse<SuperCategoryListDto>>> SearchSuperCategoriesAsync(
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
                Result<PaginatedResponse<SuperCategoryListDto>>.Failure(
                    "VALIDATION_ERROR",
                    "Search term is required."));
        }

        return GetSuperCategoriesAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm.Trim(),
            cancellationToken);
    }

    public async Task<Result<SuperCategoryListDto>> UpdateSuperCategoryAsync(
        short superCategoryId,
        UpdateSuperCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        if (superCategoryId <= 0)
        {
            return Result<SuperCategoryListDto>.Failure("VALIDATION_ERROR", "Super category ID must be greater than 0.");
        }

        var validationResult = await _updateSuperCategoryValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<SuperCategoryListDto>.ValidationFailure(errors);
        }

        var existing = await _unitOfWork.SuperCategories.GetByIdAsync(superCategoryId, cancellationToken);
        if (existing == null)
        {
            return Result<SuperCategoryListDto>.NotFound("Super category", superCategoryId);
        }

        var normalizedName = dto.SuperCategoryName.Trim();
        var isDuplicate = await _unitOfWork.SuperCategories.ExistsByNameExceptIdAsync(
            normalizedName,
            superCategoryId,
            cancellationToken);
        if (isDuplicate)
        {
            return Result<SuperCategoryListDto>.Conflict("Super category name already exists.");
        }

        // Map Status to IsDeleted
        bool? isDeleted = null;
        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            isDeleted = dto.Status.Trim().Equals("Inactive", StringComparison.OrdinalIgnoreCase);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var updated = await _unitOfWork.SuperCategories.UpdateAsync(
                superCategoryId,
                normalizedName,
                isDeleted,
                cancellationToken);

            if (isDeleted.HasValue)
            {
                await _unitOfWork.SuperCategories.UpdateRelatedStatusAsync(
                    superCategoryId,
                    isDeleted.Value,
                    cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Super category {SuperCategoryId} updated successfully.", updated.SuperCategoryId);

            return Result<SuperCategoryListDto>.Success(_mapper.Map<SuperCategoryListDto>(updated));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update super category {SuperCategoryId}", superCategoryId);
            throw;
        }
    }
}
