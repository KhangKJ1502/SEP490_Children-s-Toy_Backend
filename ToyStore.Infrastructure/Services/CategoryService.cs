using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Categories;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators.Categories;

namespace ToyStore.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CategoryService> _logger;
    private readonly CreateCategoryValidator _createCategoryValidator;
    private readonly UpdateCategoryValidator _updateCategoryValidator;

    public CategoryService(IUnitOfWork unitOfWork, ILogger<CategoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _createCategoryValidator = new CreateCategoryValidator();
        _updateCategoryValidator = new UpdateCategoryValidator();
    }

    public async Task<Result<PaginatedResponse<CategoryListDto>>> GetCategoriesAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            return Result<PaginatedResponse<CategoryListDto>>.Failure("VALIDATION_ERROR", "Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<CategoryListDto>>.Failure("VALIDATION_ERROR", "Page size must be between 1 and 100.");
        }

        var items = await _unitOfWork.Categories.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            cancellationToken);

        var totalCount = await _unitOfWork.Categories.CountAsync(searchTerm, cancellationToken);

        var mappedItems = items.Select(x => new CategoryListDto
        {
            CategoryId = x.CategoryId,
            CategoryName = x.CategoryName,
            SuperCategoryId = x.SuperCategoryId,
            SuperCategoryName = x.SuperCategoryName,
            CreatedAt = x.CreatedAt
        }).ToList();

        var response = new PaginatedResponse<CategoryListDto>(mappedItems, totalCount, pageNumber, pageSize);
        return Result<PaginatedResponse<CategoryListDto>>.Success(response);
    }

    public async Task<Result<CategoryListDto>> CreateCategoryAsync(
        CreateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _createCategoryValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<CategoryListDto>.ValidationFailure(errors);
        }

        var normalizedName = dto.CategoryName.Trim();
        var isDuplicate = await _unitOfWork.Categories.ExistsByNameAsync(normalizedName, cancellationToken);
        if (isDuplicate)
        {
            return Result<CategoryListDto>.Conflict("Category name already exists.");
        }

        var superCategory = await _unitOfWork.SuperCategories.GetByIdAsync(dto.SuperCategoryId, cancellationToken);
        if (superCategory == null)
        {
            return Result<CategoryListDto>.NotFound("Super category", dto.SuperCategoryId);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.Categories.CreateAsync(
                dto.SuperCategoryId,
                normalizedName,
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Category {CategoryId} created successfully.", created.CategoryId);

            return Result<CategoryListDto>.Success(new CategoryListDto
            {
                CategoryId = created.CategoryId,
                CategoryName = created.CategoryName,
                SuperCategoryId = superCategory.SuperCategoryId,
                SuperCategoryName = superCategory.SuperCategoryName,
                CreatedAt = created.CreatedAt
            });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create category with name {CategoryName}", dto.CategoryName);
            throw;
        }
    }

    public Task<Result<PaginatedResponse<CategoryListDto>>> SearchCategoriesAsync(
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
                Result<PaginatedResponse<CategoryListDto>>.Failure(
                    "VALIDATION_ERROR",
                    "Search term is required."));
        }

        return GetCategoriesAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm.Trim(),
            cancellationToken);
    }

    public async Task<Result<CategoryListDto>> UpdateCategoryAsync(
        short categoryId,
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        if (categoryId <= 0)
        {
            return Result<CategoryListDto>.Failure("VALIDATION_ERROR", "Category ID must be greater than 0.");
        }

        var validationResult = await _updateCategoryValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<CategoryListDto>.ValidationFailure(errors);
        }

        var existingCategory = await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken);
        if (existingCategory == null)
        {
            return Result<CategoryListDto>.NotFound("Category", categoryId);
        }

        var superCategory = await _unitOfWork.SuperCategories.GetByIdAsync(dto.SuperCategoryId, cancellationToken);
        if (superCategory == null)
        {
            return Result<CategoryListDto>.NotFound("Super category", dto.SuperCategoryId);
        }

        var normalizedName = dto.CategoryName.Trim();
        var isDuplicate = await _unitOfWork.Categories.ExistsByNameExceptIdAsync(
            normalizedName,
            categoryId,
            cancellationToken);
        if (isDuplicate)
        {
            return Result<CategoryListDto>.Conflict("Category name already exists.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var updated = await _unitOfWork.Categories.UpdateAsync(
                categoryId,
                dto.SuperCategoryId,
                normalizedName,
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Category {CategoryId} updated successfully.", updated.CategoryId);

            return Result<CategoryListDto>.Success(new CategoryListDto
            {
                CategoryId = updated.CategoryId,
                CategoryName = updated.CategoryName,
                SuperCategoryId = updated.SuperCategoryId,
                SuperCategoryName = updated.SuperCategoryName,
                CreatedAt = updated.CreatedAt
            });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update category {CategoryId}", categoryId);
            throw;
        }
    }
}