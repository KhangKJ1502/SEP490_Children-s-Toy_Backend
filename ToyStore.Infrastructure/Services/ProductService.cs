using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Products;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductService> _logger;
    private readonly IMapper _mapper;
    private readonly ICartService _cartService;
    private readonly IValidator<CreateProductDto> _createProductValidator;
    private readonly IValidator<UpdateProductDto> _updateProductValidator;
    private readonly ITimeProvider _timeProvider;

    public ProductService(
        IUnitOfWork unitOfWork,
        ILogger<ProductService> logger,
        IMapper mapper,
        ICartService cartService,
        IValidator<CreateProductDto> createProductValidator,
        IValidator<UpdateProductDto> updateProductValidator,
        ITimeProvider timeProvider)
    {
        _unitOfWork             = unitOfWork;
        _logger                 = logger;
        _mapper                 = mapper;
        _cartService            = cartService;
        _createProductValidator = createProductValidator;
        _updateProductValidator = updateProductValidator;
        _timeProvider           = timeProvider;
    }

    public async Task<Result<PaginatedResponse<ProductListDto>>> GetProductsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        short? superCategoryId = null,
        short? categoryId = null,
        IReadOnlyCollection<short>? categoryIds = null,
        IReadOnlyCollection<int>? brandIds = null,
        IReadOnlyCollection<byte>? priceRangeIds = null,
        IReadOnlyCollection<short>? materialIds = null,
        IReadOnlyCollection<byte>? ageIds = null,
        IReadOnlyCollection<byte>? sexIds = null,
        IReadOnlyCollection<byte>? originIds = null,
        int? rating = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            return Result<PaginatedResponse<ProductListDto>>.Failure("VALIDATION_ERROR", "Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<ProductListDto>>.Failure("VALIDATION_ERROR", "Page size must be between 1 and 100.");
        }

        var items = await _unitOfWork.Products.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            superCategoryId,
            categoryId,
            categoryIds,
            brandIds,
            priceRangeIds,
            materialIds,
            ageIds,
            sexIds,
            originIds,
            rating,
            status,
            cancellationToken);

        var totalCount = await _unitOfWork.Products.CountAsync(
            searchTerm,
            superCategoryId,
            categoryId,
            categoryIds,
            brandIds,
            priceRangeIds,
            materialIds,
            ageIds,
            sexIds,
            originIds,
            rating,
            status,
            cancellationToken);

        var mappedItems = _mapper.Map<List<ProductListDto>>(items);
        var response = new PaginatedResponse<ProductListDto>(mappedItems, totalCount, pageNumber, pageSize);
        return Result<PaginatedResponse<ProductListDto>>.Success(response);
    }

    public async Task<Result<ProductDto>> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
        {
            return Result<ProductDto>.Failure("VALIDATION_ERROR", "Product ID must be greater than 0.");
        }

        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            return Result<ProductDto>.NotFound("Product", productId);
        }
        var mappedProduct = _mapper.Map<ProductDto>(product);
        mappedProduct.AdditionalImageUrls = await _unitOfWork.Products.GetAdditionalImageUrlsAsync(
            productId,
            cancellationToken);
        return Result<ProductDto>.Success(mappedProduct);
    }

    public async Task<Result<ProductDto>> CreateProductAsync(
        CreateProductDto dto,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _createProductValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<ProductDto>.ValidationFailure(errors);
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result<ProductDto>.NotFound("Category", dto.CategoryId);
        }

        if (dto.BrandId.HasValue)
        {
            var brand = await _unitOfWork.Brands.GetByIdAsync(dto.BrandId.Value, cancellationToken);
            if (brand == null)
            {
                return Result<ProductDto>.NotFound("Brand", dto.BrandId.Value);
            }
        }

        if (dto.PriceRangeId.HasValue)
        {
            var exists = await _unitOfWork.Products.PriceRangeExistsAsync(dto.PriceRangeId.Value, cancellationToken);
            if (!exists)
            {
                return Result<ProductDto>.NotFound("Price range", dto.PriceRangeId.Value);
            }
        }

        if (dto.MaterialId.HasValue)
        {
            var exists = await _unitOfWork.Products.MaterialExistsAsync(dto.MaterialId.Value, cancellationToken);
            if (!exists)
            {
                return Result<ProductDto>.NotFound("Material", dto.MaterialId.Value);
            }
        }

        if (dto.AgeId.HasValue)
        {
            var exists = await _unitOfWork.Products.AgeExistsAsync(dto.AgeId.Value, cancellationToken);
            if (!exists)
            {
                return Result<ProductDto>.NotFound("Age", dto.AgeId.Value);
            }
        }

        if (dto.SexId.HasValue)
        {
            var exists = await _unitOfWork.Products.SexExistsAsync(dto.SexId.Value, cancellationToken);
            if (!exists)
            {
                return Result<ProductDto>.NotFound("Sex", dto.SexId.Value);
            }
        }

        if (dto.OriginId.HasValue)
        {
            var exists = await _unitOfWork.Products.OriginExistsAsync(dto.OriginId.Value, cancellationToken);
            if (!exists)
            {
                return Result<ProductDto>.NotFound("Origin", dto.OriginId.Value);
            }
        }

        var product = _mapper.Map<Product>(dto);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.Products.CreateAsync(
                product,
                dto.AdditionalImageUrls,
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Product {ProductId} created successfully.", created.ProductId);

            var mappedCreated = _mapper.Map<ProductDto>(created);
            mappedCreated.AdditionalImageUrls = await _unitOfWork.Products.GetAdditionalImageUrlsAsync(
                created.ProductId,
                cancellationToken);
            return Result<ProductDto>.Success(mappedCreated);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create product {ProductName}", dto.ProductName);
            throw;
        }
    }

    public async Task<Result<ProductDto>> UpdateProductAsync(
        int productId,
        UpdateProductDto dto,
        CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
        {
            return Result<ProductDto>.Failure("VALIDATION_ERROR", "Product ID must be greater than 0.");
        }

        var validationResult = await _updateProductValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<ProductDto>.ValidationFailure(errors);
        }

        if (!HasAnyUpdate(dto))
        {
            return Result<ProductDto>.Failure("VALIDATION_ERROR", "At least one field must be provided for update.");
        }

        var existing = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
        if (existing == null)
        {
            return Result<ProductDto>.NotFound("Product", productId);
        }

        if (dto.Price.HasValue && dto.Price.Value != existing.Price)
        {
            var isInPromotion = await _unitOfWork.Promotions.IsProductInActivePromotionAsync(productId, cancellationToken);
            if (isInPromotion)
            {
                return Result<ProductDto>.Failure("VALIDATION_ERROR", "Cannot change product price while it is part of an active or scheduled promotion.");
            }
        }

        if (dto.CategoryId.HasValue)
        {
            var exists = await _unitOfWork.Products.CategoryExistsAsync(dto.CategoryId.Value, cancellationToken);
            if (!exists)
            {
                return Result<ProductDto>.NotFound("Category", dto.CategoryId.Value);
            }
        }

        if (dto.BrandId.HasValue)
        {
            var exists = await _unitOfWork.Products.BrandExistsAsync(dto.BrandId.Value, cancellationToken);
            if (!exists)
            {
                return Result<ProductDto>.NotFound("Brand", dto.BrandId.Value);
            }
        }

        if (dto.PriceRangeId.HasValue)
        {
            var exists = await _unitOfWork.Products.PriceRangeExistsAsync(dto.PriceRangeId.Value, cancellationToken);
            if (!exists)
            {
                return Result<ProductDto>.NotFound("Price range", dto.PriceRangeId.Value);
            }
        }

        if (dto.MaterialId.HasValue)
        {
            var exists = await _unitOfWork.Products.MaterialExistsAsync(dto.MaterialId.Value, cancellationToken);
            if (!exists)
            {
                return Result<ProductDto>.NotFound("Material", dto.MaterialId.Value);
            }
        }

        if (dto.AgeId.HasValue)
        {
            var exists = await _unitOfWork.Products.AgeExistsAsync(dto.AgeId.Value, cancellationToken);
            if (!exists)
            {
                return Result<ProductDto>.NotFound("Age", dto.AgeId.Value);
            }
        }

        if (dto.SexId.HasValue)
        {
            var exists = await _unitOfWork.Products.SexExistsAsync(dto.SexId.Value, cancellationToken);
            if (!exists)
            {
                return Result<ProductDto>.NotFound("Sex", dto.SexId.Value);
            }
        }

        if (dto.OriginId.HasValue)
        {
            var exists = await _unitOfWork.Products.OriginExistsAsync(dto.OriginId.Value, cancellationToken);
            if (!exists)
            {
                return Result<ProductDto>.NotFound("Origin", dto.OriginId.Value);
            }
        }

        var status = dto.ProductStatus?.Trim() ?? existing.ProductStatus;
        var launchDate = dto.LaunchDate ?? existing.LaunchDate;

        if (status == "ComingSoon" && !launchDate.HasValue)
        {
            return Result<ProductDto>.BusinessError("Launch date is required for coming soon products.");
        }

        if (status == "ComingSoon" && launchDate.HasValue && launchDate.Value.Date < _timeProvider.UtcNow.Date)
        {
            return Result<ProductDto>.BusinessError("Launch date must be today or later for coming soon products.");
        }

        _mapper.Map(dto, existing);
        existing.ProductId = productId;

        if (dto.Description != null || dto.MaterialId.HasValue || dto.AgeId.HasValue || dto.SexId.HasValue || dto.OriginId.HasValue)
        {
            if (existing.ProductDetail == null)
            {
                existing.ProductDetail = new ProductDetail { ProductId = productId };
            }

            if (dto.Description != null)
            {
                existing.ProductDetail.Description = dto.Description;
            }
            if (dto.MaterialId.HasValue)
            {
                existing.ProductDetail.MaterialId = dto.MaterialId;
            }
            if (dto.AgeId.HasValue)
            {
                existing.ProductDetail.AgeId = dto.AgeId;
            }
            if (dto.SexId.HasValue)
            {
                existing.ProductDetail.SexId = dto.SexId;
            }
            if (dto.OriginId.HasValue)
            {
                existing.ProductDetail.OriginId = dto.OriginId;
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.MainImageUrl))
        {
            if (existing.ProductImage == null)
            {
                existing.ProductImage = new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = dto.MainImageUrl
                };
            }
            else
            {
                existing.ProductImage.ImageUrl = dto.MainImageUrl;
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var shouldNotifyCartRealtime = dto.Price.HasValue
                                       || dto.Quantity.HasValue
                                       || !string.IsNullOrWhiteSpace(dto.ProductStatus)
                                       || !string.IsNullOrWhiteSpace(dto.ProductName)
                                       || !string.IsNullOrWhiteSpace(dto.MainImageUrl);
        try
        {
            var updated = await _unitOfWork.Products.UpdateAsync(
                existing,
                dto.AdditionalImageUrls,
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Product {ProductId} updated successfully.", updated.ProductId);

            if (shouldNotifyCartRealtime)
            {
                await _cartService.NotifyProductChangedAsync(updated.ProductId, cancellationToken);
            }

            var mappedUpdated = _mapper.Map<ProductDto>(updated);
            mappedUpdated.AdditionalImageUrls = await _unitOfWork.Products.GetAdditionalImageUrlsAsync(
                updated.ProductId,
                cancellationToken);
            return Result<ProductDto>.Success(mappedUpdated);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update product {ProductId}", productId);
            throw;
        }
    }

    public Task<Result<PaginatedResponse<ProductListDto>>> SearchProductsAsync(
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
                Result<PaginatedResponse<ProductListDto>>.Failure(
                    "VALIDATION_ERROR",
                    "Search term is required."));
        }

        return GetProductsAsync(
            pageNumber: pageNumber,
            pageSize: pageSize,
            sortBy: sortBy,
            sortDesc: sortDesc,
            searchTerm: searchTerm.Trim(),
            cancellationToken: cancellationToken);
    }

    public async Task<Result<ProductLookupsDto>> GetProductLookupsAsync(CancellationToken cancellationToken = default)
    {
        // Lay danh sach SuperCategories
        var superCategories = await _unitOfWork.SuperCategories.GetAllAsync(cancellationToken);
        var superCategoryDtos = superCategories
            .OrderBy(x => x.SuperCategoryName)
            .Select(x => new SuperCategoryLookupDto
            {
                Id = x.SuperCategoryId,
                Label = x.SuperCategoryName
            })
            .ToList();

        // Lay danh sach Categories
        var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        var categoryDtos = categories
            .OrderBy(x => x.CategoryName)
            .Select(x => new CategoryLookupDto
            {
                Id = x.CategoryId,
                Label = x.CategoryName,
                SuperCategoryId = x.SuperCategoryId,
                SuperCategoryName = x.SuperCategory?.SuperCategoryName ?? string.Empty
            })
            .ToList();

        // Lay danh sach Brands
        var brands = await _unitOfWork.Brands.GetAllAsync(cancellationToken);
        var brandDtos = brands
            .OrderBy(x => x.BrandName)
            .Select(x => new BrandLookupDto
            {
                Id = x.BrandId,
                Label = x.BrandName
            })
            .ToList();

        // Lay danh sach PriceRanges
        var priceRanges = await _unitOfWork.Products.GetPriceRangesAsync(cancellationToken);
        var priceRangeDtos = priceRanges
            .OrderBy(x => x.PriceRangeMin)
            .Select(x => new PriceRangeLookupDto
            {
                Id = x.PriceRangeId,
                Label = $"{x.PriceRangeMin:N0} - {x.PriceRangeMax:N0} VND",
                Min = x.PriceRangeMin,
                Max = x.PriceRangeMax
            })
            .ToList();

        // Lay danh sach Materials
        var materials = await _unitOfWork.Products.GetMaterialsAsync(cancellationToken);
        var materialDtos = materials
            .OrderBy(x => x.MaterialName)
            .Select(x => new MaterialLookupDto
            {
                Id = x.MaterialId,
                Label = x.MaterialName
            })
            .ToList();

        // Lay danh sach Ages
        var ages = await _unitOfWork.Products.GetAgesAsync(cancellationToken);
        var ageDtos = ages
            .OrderBy(x => x.AgeRange)
            .Select(x => new AgeLookupDto
            {
                Id = x.AgeId,
                Label = x.AgeRange
            })
            .ToList();

        // Lay danh sach Sexes
        var sexes = await _unitOfWork.Products.GetSexesAsync(cancellationToken);
        var sexDtos = sexes
            .OrderBy(x => x.SexName)
            .Select(x => new SexLookupDto
            {
                Id = x.SexId,
                Label = x.SexName
            })
            .ToList();

        // Lay danh sach Origins
        var origins = await _unitOfWork.Products.GetOriginsAsync(cancellationToken);
        var originDtos = origins
            .OrderBy(x => x.OriginName)
            .Select(x => new OriginLookupDto
            {
                Id = x.OriginId,
                Label = x.OriginName
            })
            .ToList();

        var result = new ProductLookupsDto
        {
            SuperCategories = superCategoryDtos,
            Categories = categoryDtos,
            Brands = brandDtos,
            PriceRanges = priceRangeDtos,
            Materials = materialDtos,
            Ages = ageDtos,
            Sexes = sexDtos,
            Origins = originDtos
        };

        return Result<ProductLookupsDto>.Success(result);
    }

    private static bool HasAnyUpdate(UpdateProductDto dto)
    {
        return dto.CategoryId.HasValue
               || dto.BrandId.HasValue
               || dto.PriceRangeId.HasValue
               || !string.IsNullOrWhiteSpace(dto.ProductName)
               || dto.Price.HasValue
               || dto.Quantity.HasValue
               || !string.IsNullOrWhiteSpace(dto.ProductStatus)
               || dto.LaunchDate.HasValue
               || dto.StockThreshold.HasValue
               || dto.LowStockNotificationEnabled.HasValue
               || dto.Description != null
               || dto.MaterialId.HasValue
               || dto.AgeId.HasValue
               || dto.SexId.HasValue
               || dto.OriginId.HasValue
               || !string.IsNullOrWhiteSpace(dto.MainImageUrl)
               || dto.AdditionalImageUrls != null;
    }
}
