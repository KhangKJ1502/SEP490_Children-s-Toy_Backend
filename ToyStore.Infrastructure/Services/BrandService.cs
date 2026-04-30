using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Brands;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators.Brands;

namespace ToyStore.Infrastructure.Services;

public class BrandService : IBrandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BrandService> _logger;
    private readonly CreateBrandValidator _createBrandValidator;
    private readonly UpdateBrandValidator _updateBrandValidator;

    public BrandService(IUnitOfWork unitOfWork, ILogger<BrandService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _createBrandValidator = new CreateBrandValidator();
        _updateBrandValidator = new UpdateBrandValidator();
    }

    public async Task<Result<PaginatedResponse<BrandListDto>>> GetBrandsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            return Result<PaginatedResponse<BrandListDto>>.Failure("VALIDATION_ERROR", "Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<BrandListDto>>.Failure("VALIDATION_ERROR", "Page size must be between 1 and 100.");
        }

        var items = await _unitOfWork.Brands.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            cancellationToken);

        var totalCount = await _unitOfWork.Brands.CountAsync(searchTerm, cancellationToken);

        var mappedItems = items.Select(x => new BrandListDto
        {
            BrandId = x.BrandId,
            BrandName = x.BrandName,
            CreatedAt = x.CreatedAt
        }).ToList();

        var response = new PaginatedResponse<BrandListDto>(mappedItems, totalCount, pageNumber, pageSize);
        return Result<PaginatedResponse<BrandListDto>>.Success(response);
    }

    public async Task<Result<BrandListDto>> CreateBrandAsync(
        CreateBrandDto dto,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _createBrandValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<BrandListDto>.ValidationFailure(errors);
        }

        var normalizedName = dto.BrandName.Trim();
        var isDuplicate = await _unitOfWork.Brands.ExistsByNameAsync(normalizedName, cancellationToken);
        if (isDuplicate)
        {
            return Result<BrandListDto>.Conflict("Brand name already exists.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.Brands.CreateAsync(normalizedName, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Brand {BrandId} created successfully.", created.BrandId);

            return Result<BrandListDto>.Success(new BrandListDto
            {
                BrandId = created.BrandId,
                BrandName = created.BrandName,
                CreatedAt = created.CreatedAt
            });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create brand with name {BrandName}", dto.BrandName);
            throw;
        }
    }

    public Task<Result<PaginatedResponse<BrandListDto>>> SearchBrandsAsync(
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
                Result<PaginatedResponse<BrandListDto>>.Failure(
                    "VALIDATION_ERROR",
                    "Search term is required."));
        }

        return GetBrandsAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm.Trim(),
            cancellationToken);
    }

    public async Task<Result<BrandListDto>> UpdateBrandAsync(
        short brandId,
        UpdateBrandDto dto,
        CancellationToken cancellationToken = default)
    {
        if (brandId <= 0)
        {
            return Result<BrandListDto>.Failure("VALIDATION_ERROR", "Brand ID must be greater than 0.");
        }

        var validationResult = await _updateBrandValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<BrandListDto>.ValidationFailure(errors);
        }

        var existing = await _unitOfWork.Brands.GetByIdAsync(brandId, cancellationToken);
        if (existing == null)
        {
            return Result<BrandListDto>.NotFound("Brand", brandId);
        }

        var normalizedName = dto.BrandName.Trim();
        var isDuplicate = await _unitOfWork.Brands.ExistsByNameExceptIdAsync(
            normalizedName,
            brandId,
            cancellationToken);
        if (isDuplicate)
        {
            return Result<BrandListDto>.Conflict("Brand name already exists.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var updated = await _unitOfWork.Brands.UpdateAsync(
                brandId,
                normalizedName,
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Brand {BrandId} updated successfully.", updated.BrandId);

            return Result<BrandListDto>.Success(new BrandListDto
            {
                BrandId = updated.BrandId,
                BrandName = updated.BrandName,
                CreatedAt = updated.CreatedAt
            });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update brand {BrandId}", brandId);
            throw;
        }
    }
}
