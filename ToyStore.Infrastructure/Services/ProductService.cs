using AutoMapper;
using ClosedXML.Excel;
using FluentValidation;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;
using ToyStore.Application.Common.Helpers;
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
        decimal? minPrice = null,
        decimal? maxPrice = null,
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
            minPrice,
            maxPrice,
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
            minPrice,
            maxPrice,
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

        var referenceErrors = await ValidateCreateProductReferencesAsync(dto, cancellationToken);
        if (referenceErrors.Count > 0)
        {
            return Result<ProductDto>.ValidationFailure(referenceErrors);
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

        if (dto.Description != null || dto.MaterialId.HasValue || dto.AgeId.HasValue || dto.SexId.HasValue || dto.OriginId.HasValue
            || dto.WeightGram.HasValue || dto.LengthCm.HasValue || dto.WidthCm.HasValue || dto.HeightCm.HasValue)
        {
            if (existing.ProductDetail == null)
            {
                existing.ProductDetail = new ProductDetail
                {
                    ProductId = productId,
                    WeightGram = dto.WeightGram ?? 1,
                    LengthCm = dto.LengthCm ?? 1,
                    WidthCm = dto.WidthCm ?? 1,
                    HeightCm = dto.HeightCm ?? 1
                };
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
            if (dto.WeightGram.HasValue)
            {
                existing.ProductDetail.WeightGram = dto.WeightGram.Value;
            }
            if (dto.LengthCm.HasValue)
            {
                existing.ProductDetail.LengthCm = dto.LengthCm.Value;
            }
            if (dto.WidthCm.HasValue)
            {
                existing.ProductDetail.WidthCm = dto.WidthCm.Value;
            }
            if (dto.HeightCm.HasValue)
            {
                existing.ProductDetail.HeightCm = dto.HeightCm.Value;
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

    public async Task<Result<ProductQuantityReportFileDto>> ExportProductQuantityReportAsync(
        ProductQuantityReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return Result<ProductQuantityReportFileDto>.Failure("VALIDATION_ERROR", "Report request is required.");
        }

        var format = (request.Format ?? "pdf").Trim().ToLowerInvariant();
        if (format is not ("pdf" or "xlsx" or "csv"))
        {
            return Result<ProductQuantityReportFileDto>.Failure("VALIDATION_ERROR", "Report format is invalid.");
        }

        if (request.DateFrom.HasValue && request.DateTo.HasValue && request.DateFrom.Value > request.DateTo.Value)
        {
            return Result<ProductQuantityReportFileDto>.Failure("VALIDATION_ERROR", "Date range is invalid.");
        }

        var dateField = request.DateField?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(dateField) && dateField is not ("createdat" or "updatedat"))
        {
            return Result<ProductQuantityReportFileDto>.Failure("VALIDATION_ERROR", "Date field is invalid.");
        }

        var dateFrom = request.DateFrom;
        var dateTo = request.DateTo;
        if (dateTo.HasValue && dateTo.Value.TimeOfDay == TimeSpan.Zero)
        {
            dateTo = dateTo.Value.Date.AddDays(1).AddTicks(-1);
        }

        var products = await _unitOfWork.Products.GetProductQuantityReportAsync(
            sortBy: request.SortBy,
            sortDesc: request.SortDesc,
            searchTerm: request.SearchTerm,
            categoryId: request.CategoryId,
            brandId: request.BrandId,
            priceRangeId: request.PriceRangeId,
            materialId: request.MaterialId,
            ageId: request.AgeId,
            originId: request.OriginId,
            status: request.Status,
            lowStockOnly: request.LowStockOnly,
            dateFrom: dateFrom,
            dateTo: dateTo,
            dateField: dateField,
            cancellationToken: cancellationToken);

        var items = _mapper.Map<List<ProductQuantityReportItemDto>>(products);
        var summary = BuildProductQuantitySummary(items);
        var generatedAt = _timeProvider.VnNow;
        var filterDescriptions = BuildFilterDescriptions(request, dateFrom, dateTo);

        var file = format switch
        {
            "pdf" => BuildPdfReport(items, summary, generatedAt, filterDescriptions),
            "xlsx" => BuildXlsxReport(items, summary, generatedAt, filterDescriptions),
            "csv" => BuildCsvReport(items),
            _ => new ProductQuantityReportFileDto()
        };

        return Result<ProductQuantityReportFileDto>.Success(file);
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
               || dto.WeightGram.HasValue
               || dto.LengthCm.HasValue
               || dto.WidthCm.HasValue
               || dto.HeightCm.HasValue
               || !string.IsNullOrWhiteSpace(dto.MainImageUrl)
               || dto.AdditionalImageUrls != null;
    }

    private static ProductQuantityReportSummaryDto BuildProductQuantitySummary(IReadOnlyCollection<ProductQuantityReportItemDto> items)
    {
        return new ProductQuantityReportSummaryDto
        {
            TotalProducts = items.Count,
            TotalQuantity = items.Sum(x => x.Quantity),
            TotalProductValue = items.Sum(x => x.ProductValue),
            LowStockCount = items.Count(x => x.LowStock),
            OutOfStockCount = items.Count(x => x.Quantity == 0)
        };
    }

    private static List<string> BuildFilterDescriptions(
        ProductQuantityReportRequestDto request,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            filters.Add($"Search: {request.SearchTerm}");
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            filters.Add($"Status: {request.Status}");
        }

        if (request.CategoryId.HasValue)
        {
            filters.Add($"Category ID: {request.CategoryId}");
        }

        if (request.BrandId.HasValue)
        {
            filters.Add($"Brand ID: {request.BrandId}");
        }

        if (request.PriceRangeId.HasValue)
        {
            filters.Add($"Price range ID: {request.PriceRangeId}");
        }

        if (request.MaterialId.HasValue)
        {
            filters.Add($"Material ID: {request.MaterialId}");
        }

        if (request.AgeId.HasValue)
        {
            filters.Add($"Age ID: {request.AgeId}");
        }

        if (request.OriginId.HasValue)
        {
            filters.Add($"Origin ID: {request.OriginId}");
        }

        if (request.LowStockOnly)
        {
            filters.Add("Low stock only");
        }

        if (dateFrom.HasValue || dateTo.HasValue)
        {
            var field = string.Equals(request.DateField, "updatedat", StringComparison.OrdinalIgnoreCase)
                ? "Updated"
                : "Created";
            var fromText = dateFrom.HasValue ? dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "Any";
            var toText = dateTo.HasValue ? dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "Any";
            filters.Add($"{field} date: {fromText} - {toText}");
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var direction = request.SortDesc ? "DESC" : "ASC";
            filters.Add($"Sort: {request.SortBy} {direction}");
        }

        return filters;
    }

    private static ProductQuantityReportFileDto BuildCsvReport(IReadOnlyCollection<ProductQuantityReportItemDto> items)
    {
        var builder = new StringBuilder();
        var headers = new[]
        {
            "ProductId",
            "ProductName",
            "Category",
            "Brand",
            "ProductStatus",
            "Status",
            "Price",
            "DiscountedPrice",
            "DiscountPercent",
            "PromotionType",
            "Quantity",
            "StockThreshold",
            "LowStock",
            "ProductValue",
            "SoldQuantity",
            "ReviewCount",
            "AverageRating",
            "CreatedAt",
            "UpdatedAt"
        };

        builder.AppendLine(string.Join(",", headers));

        foreach (var item in items)
        {
            var row = new[]
            {
                item.ProductId.ToString(CultureInfo.InvariantCulture),
                EscapeCsv(item.ProductName),
                EscapeCsv(item.CategoryName),
                EscapeCsv(item.BrandName),
                EscapeCsv(item.ProductStatus),
                EscapeCsv(item.Status),
                item.Price.ToString(CultureInfo.InvariantCulture),
                item.DiscountedPrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                item.DiscountPercent?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                EscapeCsv(item.PromotionType),
                item.Quantity.ToString(CultureInfo.InvariantCulture),
                item.StockThreshold.ToString(CultureInfo.InvariantCulture),
                item.LowStock ? "Yes" : "No",
                item.ProductValue.ToString(CultureInfo.InvariantCulture),
                item.SoldQuantity.ToString(CultureInfo.InvariantCulture),
                item.ReviewCount.ToString(CultureInfo.InvariantCulture),
                item.AverageRating?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                item.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                item.UpdatedAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? string.Empty
            };

            builder.AppendLine(string.Join(",", row));
        }

        return new ProductQuantityReportFileDto
        {
            Content = Encoding.UTF8.GetBytes(builder.ToString()),
            ContentType = "text/csv",
            FileName = "product-quantity-report.csv"
        };
    }

    private static ProductQuantityReportFileDto BuildXlsxReport(
        IReadOnlyCollection<ProductQuantityReportItemDto> items,
        ProductQuantityReportSummaryDto summary,
        DateTime generatedAt,
        IReadOnlyCollection<string> filters)
    {
        using var workbook = new XLWorkbook();
        var summarySheet = workbook.AddWorksheet("Summary");
        summarySheet.Cell("A1").Value = "Product Quantity Report";
        summarySheet.Cell("A1").Style.Font.Bold = true;
        summarySheet.Cell("A2").Value = "Generated at";
        summarySheet.Cell("B2").Value = generatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        summarySheet.Cell("A4").Value = "Total products";
        summarySheet.Cell("B4").Value = summary.TotalProducts;
        summarySheet.Cell("A5").Value = "Total quantity";
        summarySheet.Cell("B5").Value = summary.TotalQuantity;
        summarySheet.Cell("A6").Value = "Total product value";
        summarySheet.Cell("B6").Value = summary.TotalProductValue;
        summarySheet.Cell("A7").Value = "Low stock count";
        summarySheet.Cell("B7").Value = summary.LowStockCount;
        summarySheet.Cell("A8").Value = "Out of stock count";
        summarySheet.Cell("B8").Value = summary.OutOfStockCount;

        if (filters.Count > 0)
        {
            summarySheet.Cell("A10").Value = "Filters";
            summarySheet.Cell("A10").Style.Font.Bold = true;
            var row = 11;
            foreach (var filter in filters)
            {
                summarySheet.Cell(row, 1).Value = filter;
                row++;
            }
        }

        summarySheet.Columns().AdjustToContents();

        var dataSheet = workbook.AddWorksheet("Data");
        var headers = new[]
        {
            "ProductId",
            "ProductName",
            "Category",
            "Brand",
            "ProductStatus",
            "Status",
            "Price",
            "DiscountedPrice",
            "DiscountPercent",
            "PromotionType",
            "Quantity",
            "StockThreshold",
            "LowStock",
            "ProductValue",
            "SoldQuantity",
            "ReviewCount",
            "AverageRating",
            "CreatedAt",
            "UpdatedAt"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            dataSheet.Cell(1, i + 1).Value = headers[i];
            dataSheet.Cell(1, i + 1).Style.Font.Bold = true;
            dataSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
        }

        var rowIndex = 2;
        foreach (var item in items)
        {
            dataSheet.Cell(rowIndex, 1).Value = item.ProductId;
            dataSheet.Cell(rowIndex, 2).Value = item.ProductName;
            dataSheet.Cell(rowIndex, 3).Value = item.CategoryName;
            dataSheet.Cell(rowIndex, 4).Value = item.BrandName ?? string.Empty;
            dataSheet.Cell(rowIndex, 5).Value = item.ProductStatus;
            dataSheet.Cell(rowIndex, 6).Value = item.Status;
            dataSheet.Cell(rowIndex, 7).Value = item.Price;
            dataSheet.Cell(rowIndex, 8).Value = item.DiscountedPrice;
            dataSheet.Cell(rowIndex, 9).Value = item.DiscountPercent;
            dataSheet.Cell(rowIndex, 10).Value = item.PromotionType ?? string.Empty;
            dataSheet.Cell(rowIndex, 11).Value = item.Quantity;
            dataSheet.Cell(rowIndex, 12).Value = item.StockThreshold;
            dataSheet.Cell(rowIndex, 13).Value = item.LowStock ? "Yes" : "No";
            dataSheet.Cell(rowIndex, 14).Value = item.ProductValue;
            dataSheet.Cell(rowIndex, 15).Value = item.SoldQuantity;
            dataSheet.Cell(rowIndex, 16).Value = item.ReviewCount;
            dataSheet.Cell(rowIndex, 17).Value = item.AverageRating;
            dataSheet.Cell(rowIndex, 18).Value = item.CreatedAt;
            dataSheet.Cell(rowIndex, 19).Value = item.UpdatedAt;
            rowIndex++;
        }

        dataSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ProductQuantityReportFileDto
        {
            Content = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = "product-quantity-report.xlsx"
        };
    }

    private static ProductQuantityReportFileDto BuildPdfReport(
        IReadOnlyCollection<ProductQuantityReportItemDto> items,
        ProductQuantityReportSummaryDto summary,
        DateTime generatedAt,
        IReadOnlyCollection<string> filters)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(header => BuildPdfHeader(header, generatedAt));
                page.Content().Element(content => BuildPdfContent(content, items, summary, filters));
                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });

        return new ProductQuantityReportFileDto
        {
            Content = document.GeneratePdf(),
            ContentType = "application/pdf",
            FileName = "product-quantity-report.pdf"
        };
    }

    private static void BuildPdfHeader(IContainer container, DateTime generatedAt)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("ToyStore").FontSize(18).SemiBold().FontColor("#0F172A");
                column.Item().Text("Product Quantity Report").FontSize(12).FontColor("#475569");
            });

            row.ConstantItem(220).Column(column =>
            {
                column.Item().Text("Generated at").FontSize(9).FontColor("#64748B");
                column.Item().Text(generatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                    .FontSize(10)
                    .FontColor("#0F172A");
            });
        });
    }

    private static void BuildPdfContent(
        IContainer container,
        IReadOnlyCollection<ProductQuantityReportItemDto> items,
        ProductQuantityReportSummaryDto summary,
        IReadOnlyCollection<string> filters)
    {
        container.Column(column =>
        {
            column.Spacing(12);

            column.Item().Element(summaryContainer => BuildPdfSummary(summaryContainer, summary));

            if (filters.Count > 0)
            {
                column.Item().Element(filterContainer => BuildPdfFilters(filterContainer, filters));
            }

            column.Item().Element(tableContainer => BuildPdfTable(tableContainer, items));
        });
    }

    private static void BuildPdfSummary(IContainer container, ProductQuantityReportSummaryDto summary)
    {
        container.Row(row =>
        {
            row.Spacing(10);
            BuildPdfSummaryCard(row.RelativeItem(), "Total products", summary.TotalProducts.ToString(CultureInfo.InvariantCulture));
            BuildPdfSummaryCard(row.RelativeItem(), "Total quantity", summary.TotalQuantity.ToString(CultureInfo.InvariantCulture));
            BuildPdfSummaryCard(row.RelativeItem(), "Product value", MoneyHelper.FormatVND(summary.TotalProductValue));
            BuildPdfSummaryCard(row.RelativeItem(), "Low stock", summary.LowStockCount.ToString(CultureInfo.InvariantCulture));
            BuildPdfSummaryCard(row.RelativeItem(), "Out of stock", summary.OutOfStockCount.ToString(CultureInfo.InvariantCulture));
        });
    }

    private static void BuildPdfSummaryCard(IContainer container, string label, string value)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten3).Padding(8).Column(column =>
        {
            column.Item().Text(label).FontSize(9).FontColor("#64748B");
            column.Item().Text(value).FontSize(12).SemiBold().FontColor("#0F172A");
        });
    }

    private static void BuildPdfFilters(IContainer container, IReadOnlyCollection<string> filters)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten3).Padding(8).Column(column =>
        {
            column.Item().Text("Filters").FontSize(10).SemiBold().FontColor("#0F172A");
            foreach (var filter in filters)
            {
                column.Item().Text(filter).FontSize(9).FontColor("#475569");
            }
        });
    }

    private static void BuildPdfTable(IContainer container, IReadOnlyCollection<ProductQuantityReportItemDto> items)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(36);
                columns.RelativeColumn(2);
                columns.RelativeColumn(1.4f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(0.9f);
                columns.RelativeColumn(1.0f);
                columns.RelativeColumn(1.0f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.2f);
            });

            table.Header(header =>
            {
                header.Cell().Element(PdfHeaderCellStyle).Text("ID");
                header.Cell().Element(PdfHeaderCellStyle).Text("Product");
                header.Cell().Element(PdfHeaderCellStyle).Text("Category");
                header.Cell().Element(PdfHeaderCellStyle).Text("Brand");
                header.Cell().Element(PdfHeaderCellStyle).Text("Status");
                header.Cell().Element(PdfHeaderCellStyle).Text("Price");
                header.Cell().Element(PdfHeaderCellStyle).Text("Sale Price");
                header.Cell().Element(PdfHeaderCellStyle).Text("Discount");
                header.Cell().Element(PdfHeaderCellStyle).Text("Promo");
                header.Cell().Element(PdfHeaderCellStyle).Text("Qty");
                header.Cell().Element(PdfHeaderCellStyle).Text("Threshold");
                header.Cell().Element(PdfHeaderCellStyle).Text("Low Stock");
                header.Cell().Element(PdfHeaderCellStyle).Text("Value");
                header.Cell().Element(PdfHeaderCellStyle).Text("Sold");
                header.Cell().Element(PdfHeaderCellStyle).Text("Updated");
            });

            foreach (var item in items)
            {
                table.Cell().Element(PdfBodyCellStyle).Text(item.ProductId.ToString(CultureInfo.InvariantCulture));
                table.Cell().Element(PdfBodyCellStyle).Text(item.ProductName);
                table.Cell().Element(PdfBodyCellStyle).Text(item.CategoryName);
                table.Cell().Element(PdfBodyCellStyle).Text(item.BrandName ?? "-");
                table.Cell().Element(PdfBodyCellStyle).Text(item.ProductStatus);
                table.Cell().Element(PdfBodyCellStyle).Text(MoneyHelper.FormatVND(item.Price));
                table.Cell().Element(PdfBodyCellStyle).Text(item.DiscountedPrice.HasValue
                    ? MoneyHelper.FormatVND(item.DiscountedPrice.Value)
                    : "-");
                table.Cell().Element(PdfBodyCellStyle).Text(item.DiscountPercent.HasValue
                    ? $"{item.DiscountPercent.Value}%"
                    : "-");
                table.Cell().Element(PdfBodyCellStyle).Text(item.PromotionType ?? "-");
                table.Cell().Element(PdfBodyCellStyle).Text(item.Quantity.ToString(CultureInfo.InvariantCulture));
                table.Cell().Element(PdfBodyCellStyle).Text(item.StockThreshold.ToString(CultureInfo.InvariantCulture));
                table.Cell().Element(PdfBodyCellStyle).Text(item.LowStock ? "Yes" : "No");
                table.Cell().Element(PdfBodyCellStyle).Text(MoneyHelper.FormatVND(item.ProductValue));
                table.Cell().Element(PdfBodyCellStyle).Text(item.SoldQuantity.ToString(CultureInfo.InvariantCulture));
                table.Cell().Element(PdfBodyCellStyle).Text(item.UpdatedAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-");
            }
        });
    }

    private static IContainer PdfHeaderCellStyle(IContainer container)
    {
        return container
            .DefaultTextStyle(x => x.SemiBold().FontColor("#0F172A").FontSize(9))
            .PaddingVertical(6)
            .PaddingHorizontal(4)
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2);
    }

    private static IContainer PdfBodyCellStyle(IContainer container)
    {
        return container
            .PaddingVertical(4)
            .PaddingHorizontal(4)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten4);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        var escaped = value.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }

    private async Task<Dictionary<string, string[]>> ValidateCreateProductReferencesAsync(
        CreateProductDto dto,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (!await _unitOfWork.Products.CategoryExistsAsync(dto.CategoryId, cancellationToken))
        {
            errors[nameof(dto.CategoryId)] = ["Selected category does not exist."];
        }

        if (dto.BrandId.HasValue && !await _unitOfWork.Products.BrandExistsAsync(dto.BrandId.Value, cancellationToken))
        {
            errors[nameof(dto.BrandId)] = ["Selected brand does not exist."];
        }

        if (dto.PriceRangeId.HasValue && !await _unitOfWork.Products.PriceRangeExistsAsync(dto.PriceRangeId.Value, cancellationToken))
        {
            errors[nameof(dto.PriceRangeId)] = ["Selected price range does not exist."];
        }
        else if (dto.PriceRangeId.HasValue)
        {
            var priceRange = (await _unitOfWork.Products.GetPriceRangesAsync(cancellationToken))
                .FirstOrDefault(range => range.PriceRangeId == dto.PriceRangeId.Value);

            if (priceRange != null && (dto.Price < priceRange.PriceRangeMin || dto.Price > priceRange.PriceRangeMax))
            {
                errors[nameof(dto.PriceRangeId)] = ["Selected price range does not match the entered price."];
            }
        }

        if (dto.MaterialId.HasValue && !await _unitOfWork.Products.MaterialExistsAsync(dto.MaterialId.Value, cancellationToken))
        {
            errors[nameof(dto.MaterialId)] = ["Selected material does not exist."];
        }

        if (dto.AgeId.HasValue && !await _unitOfWork.Products.AgeExistsAsync(dto.AgeId.Value, cancellationToken))
        {
            errors[nameof(dto.AgeId)] = ["Selected age range does not exist."];
        }

        if (dto.SexId.HasValue && !await _unitOfWork.Products.SexExistsAsync(dto.SexId.Value, cancellationToken))
        {
            errors[nameof(dto.SexId)] = ["Selected sex does not exist."];
        }

        if (dto.OriginId.HasValue && !await _unitOfWork.Products.OriginExistsAsync(dto.OriginId.Value, cancellationToken))
        {
            errors[nameof(dto.OriginId)] = ["Selected origin does not exist."];
        }

        return errors;
    }
}
