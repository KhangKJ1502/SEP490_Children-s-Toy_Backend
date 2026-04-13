using ToyStore.Application.Common.Helpers;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Product service implementation using Result pattern.
/// This version doesn't throw exceptions - it returns Result objects instead.
/// </summary>
public class ProductServiceV2 : IProductServiceV2
{
    private readonly IUnitOfWork _unitOfWork;
    
    public ProductServiceV2(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Validation
        if (id == Guid.Empty)
            return Result<ProductDto>.ValidationFailure(new Dictionary<string, string[]>
            {
                { "Id", new[] { "ID sản phẩm không hợp lệ." } }
            });
        
        // Get product
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        
        if (product == null)
            return Result<ProductDto>.NotFound("Sản phẩm", id);
        
        return Result<ProductDto>.Success(MapToDto(product));
    }
    
    public async Task<Result<ProductDto>> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Validate input
        var validationResult = CreateProductValidator.Validate(dto);
        if (!validationResult.IsValid)
            return Result<ProductDto>.ValidationFailure(validationResult.ToErrorDictionary());
        
        // Step 2: Check duplicate SKU
        var existingBySku = await _unitOfWork.Products.GetBySKUAsync(dto.SKU, cancellationToken);
        if (existingBySku != null)
            return Result<ProductDto>.Conflict($"Mã SKU '{dto.SKU}' đã tồn tại.");
        
        // Step 3: Generate unique slug
        var baseSlug = StringHelper.GenerateSlug(dto.Name);
        var slug = baseSlug;
        var counter = 1;
        
        while (await _unitOfWork.Products.GetBySlugAsync(slug, cancellationToken) != null)
        {
            slug = $"{baseSlug}-{counter++}";
            if (counter > 100)
                return Result<ProductDto>.BusinessError("Không thể tạo slug duy nhất cho sản phẩm.");
        }
        
        // Step 4: Create product
        var product = new Product
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Description = dto.Description?.Trim(),
            ShortDescription = dto.ShortDescription?.Trim(),
            SKU = dto.SKU.ToUpperInvariant().Trim(),
            Price = dto.Price,
            SalePrice = dto.SalePrice,
            CostPrice = dto.CostPrice,
            CategoryId = dto.CategoryId,
            ToyCategory = dto.ToyCategory,
            AgeRange = dto.AgeRange,
            MinAge = dto.MinAge,
            MaxAge = dto.MaxAge,
            Brand = dto.Brand?.Trim(),
            Weight = dto.Weight,
            Dimensions = dto.Dimensions?.Trim(),
            ImageUrl = dto.ImageUrl?.Trim(),
            AdditionalImages = dto.AdditionalImages != null 
                ? string.Join(",", dto.AdditionalImages) 
                : null,
            StockQuantity = dto.StockQuantity,
            LowStockThreshold = dto.LowStockThreshold,
            IsFeatured = dto.IsFeatured,
            IsNewArrival = dto.IsNewArrival,
            Tags = dto.Tags != null ? string.Join(",", dto.Tags) : null,
            IsActive = true
        };
        
        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result<ProductDto>.Success(MapToDto(product));
    }
    
    public async Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Validate ID
        if (id == Guid.Empty)
            return Result<ProductDto>.ValidationFailure(new Dictionary<string, string[]>
            {
                { "Id", new[] { "ID sản phẩm không hợp lệ." } }
            });
        
        // Step 2: Get product
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        if (product == null)
            return Result<ProductDto>.NotFound("Sản phẩm", id);
        
        // Step 3: Validate price if provided
        if (dto.Price.HasValue && dto.Price.Value <= 0)
            return Result<ProductDto>.ValidationFailure(new Dictionary<string, string[]>
            {
                { "Price", new[] { "Giá phải lớn hơn 0." } }
            });
            
        if (dto.SalePrice.HasValue && dto.Price.HasValue && dto.SalePrice >= dto.Price)
            return Result<ProductDto>.ValidationFailure(new Dictionary<string, string[]>
            {
                { "SalePrice", new[] { "Giá khuyến mãi phải nhỏ hơn giá gốc." } }
            });
        
        // Step 4: Update fields
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            product.Name = dto.Name.Trim();
            product.Slug = StringHelper.GenerateSlug(dto.Name);
        }
        if (dto.Description != null) product.Description = dto.Description.Trim();
        if (dto.ShortDescription != null) product.ShortDescription = dto.ShortDescription.Trim();
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.SalePrice.HasValue) product.SalePrice = dto.SalePrice;
        if (dto.CategoryId.HasValue) product.CategoryId = dto.CategoryId.Value;
        if (dto.ToyCategory.HasValue) product.ToyCategory = dto.ToyCategory.Value;
        if (dto.AgeRange.HasValue) product.AgeRange = dto.AgeRange.Value;
        if (dto.StockQuantity.HasValue) product.StockQuantity = dto.StockQuantity.Value;
        if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;
        if (dto.IsFeatured.HasValue) product.IsFeatured = dto.IsFeatured.Value;
        
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result<ProductDto>.Success(MapToDto(product));
    }
    
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Step 1: Validate ID
        if (id == Guid.Empty)
            return Result.ValidationFailure(new Dictionary<string, string[]>
            {
                { "Id", new[] { "ID sản phẩm không hợp lệ." } }
            });
        
        // Step 2: Get product
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        if (product == null)
            return Result.NotFound("Sản phẩm", id);
        
        // Step 3: Check if already deleted
        if (product.IsDeleted)
            return Result.BusinessError("Sản phẩm đã bị xóa trước đó.");
        
        // Step 4: Soft delete
        product.IsDeleted = true;
        product.DeletedAt = DateTime.UtcNow;
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
    
    #region Private Methods
    
    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            SKU = product.SKU,
            Price = product.Price,
            SalePrice = product.SalePrice,
            EffectivePrice = product.EffectivePrice,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? "",
            ToyCategory = product.ToyCategory,
            AgeRange = product.AgeRange,
            MinAge = product.MinAge,
            MaxAge = product.MaxAge,
            Brand = product.Brand,
            ImageUrl = product.ImageUrl,
            AdditionalImages = product.AdditionalImages?.Split(',').ToList() ?? new(),
            StockQuantity = product.StockQuantity,
            IsInStock = product.IsInStock,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            IsNewArrival = product.IsNewArrival,
            AverageRating = product.AverageRating,
            ReviewCount = product.ReviewCount,
            Tags = product.Tags?.Split(',').ToList() ?? new(),
            CreatedAt = product.CreatedAt
        };
    }
    
    #endregion
}
