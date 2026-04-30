using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Brands;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

public class BrandService : IBrandService
{
    private const string InactiveStatus = "Inactive";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<BrandService> _logger;
    private readonly IValidator<CreateBrandDto> _createBrandValidator;
    private readonly IValidator<UpdateBrandDto> _updateBrandValidator;

    public BrandService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<BrandService> logger,
        IValidator<CreateBrandDto> createBrandValidator,
        IValidator<UpdateBrandDto> updateBrandValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _createBrandValidator = createBrandValidator;
        _updateBrandValidator = updateBrandValidator;
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

        var mappedItems = _mapper.Map<List<BrandListDto>>(items);

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

            return Result<BrandListDto>.Success(_mapper.Map<BrandListDto>(created));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create brand with name {BrandName}", dto.BrandName);
            throw;
        }
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
        var isDeleted = ParseStatusToIsDeleted(dto.Status);

        var hasNameChanged = !string.Equals(existing.BrandName, normalizedName, StringComparison.Ordinal);
        var hasStatusChanged = existing.IsDeleted != isDeleted;

        if (!hasNameChanged && !hasStatusChanged)
        {
            return Result<BrandListDto>.Success(_mapper.Map<BrandListDto>(existing));
        }

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
                isDeleted,
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Brand {BrandId} updated successfully.", updated.BrandId);

            return Result<BrandListDto>.Success(_mapper.Map<BrandListDto>(updated));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update brand {BrandId}", brandId);
            throw;
        }
    }

    private static bool ParseStatusToIsDeleted(string status)
    {
        return string.Equals(
            status.Trim(),
            InactiveStatus,
            StringComparison.OrdinalIgnoreCase);
    }
}
