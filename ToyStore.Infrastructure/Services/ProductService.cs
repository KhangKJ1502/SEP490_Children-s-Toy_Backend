using ToyStore.Application.Common.Exceptions;
using ToyStore.Application.Common.Helpers;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Product service implementation with validation and error handling.
/// </summary>
public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }
    
    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Id", "ID sản phẩm không hợp lệ.");
            
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        return product != null ? MapToDto(product) : null;
    }
    
    public async Task<ProductDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ValidationException("Slug", "Slug không được để trống.");
            
        var product = await _unitOfWork.Products.GetBySlugAsync(slug, cancellationToken);
        return product != null ? MapToDto(product) : null;
    }
    
    public async Task<PaginatedResponse<ProductListDto>> GetProductsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? categoryId = null,
        ToyCategory? toyCategory = null,
        AgeRange? ageRange = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (pageNumber < 1)
            throw new ValidationException("PageNumber", "Số trang phải lớn hơn hoặc bằng 1.");
        if (pageSize < 1 || pageSize > 100)
            throw new ValidationException("PageSize", "Kích thước trang phải từ 1 đến 100.");
        if (minPrice.HasValue && minPrice < 0)
            throw new ValidationException("MinPrice", "Giá tối thiểu không được âm.");
        if (maxPrice.HasValue && maxPrice < 0)
            throw new ValidationException("MaxPrice", "Giá tối đa không được âm.");
        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            throw new ValidationException("Price", "Giá tối thiểu phải nhỏ hơn giá tối đa.");
            
        var (products, totalCount) = await _unitOfWork.Products.GetPagedAsync(
            pageNumber, pageSize, categoryId, toyCategory, ageRange,
            minPrice, maxPrice, searchTerm, sortBy, sortDescending, cancellationToken);
            
        var items = products.Select(MapToListDto).ToList();
        return new PaginatedResponse<ProductListDto>(items, totalCount, pageNumber, pageSize);
    }
    
    public async Task<IReadOnlyList<ProductListDto>> GetFeaturedProductsAsync(
        int limit = 10, CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 50)
            throw new ValidationException("Limit", "Giới hạn phải từ 1 đến 50.");
            
        var products = await _unitOfWork.Products.GetFeaturedAsync(limit, cancellationToken);
        return products.Select(MapToListDto).ToList();
    }
    
    public async Task<IReadOnlyList<ProductListDto>> GetNewArrivalsAsync(
        int limit = 10, CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 50)
            throw new ValidationException("Limit", "Giới hạn phải từ 1 đến 50.");
            
        var products = await _unitOfWork.Products.GetNewArrivalsAsync(limit, cancellationToken);
        return products.Select(MapToListDto).ToList();
    }
    
    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        // Validate input
        var validationResult = CreateProductValidator.Validate(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToErrorDictionary());
        
        // Check for duplicate SKU
        var existingBySku = await _unitOfWork.Products.GetBySKUAsync(dto.SKU, cancellationToken);
        if (existingBySku != null)
            throw ConflictException.DuplicateEntry("SKU", dto.SKU);
            
        // Generate unique slug
        var baseSlug = StringHelper.GenerateSlug(dto.Name);
        var slug = baseSlug;
        var counter = 1;
        
        while (await _unitOfWork.Products.GetBySlugAsync(slug, cancellationToken) != null)
        {
            slug = $"{baseSlug}-{counter++}";
            if (counter > 100)
                throw new BusinessRuleException("Không thể tạo slug duy nhất cho sản phẩm.");
        }
        
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
        
        return MapToDto(product);
    }
    
    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Id", "ID sản phẩm không hợp lệ.");
            
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        if (product == null)
            throw NotFoundException.For<Product>(id);
        
        // Validate price if provided
        if (dto.Price.HasValue && dto.Price.Value <= 0)
            throw new ValidationException("Price", "Giá phải lớn hơn 0.");
            
        if (dto.SalePrice.HasValue && dto.Price.HasValue && dto.SalePrice >= dto.Price)
            throw new ValidationException("SalePrice", "Giá khuyến mãi phải nhỏ hơn giá gốc.");
            
        if (dto.StockQuantity.HasValue && dto.StockQuantity < 0)
            throw new ValidationException("StockQuantity", "Số lượng tồn kho không được âm.");
        
        // Update fields
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
        if (dto.MinAge.HasValue) product.MinAge = dto.MinAge;
        if (dto.MaxAge.HasValue) product.MaxAge = dto.MaxAge;
        if (dto.Brand != null) product.Brand = dto.Brand.Trim();
        if (dto.ImageUrl != null) product.ImageUrl = dto.ImageUrl.Trim();
        if (dto.AdditionalImages != null) 
            product.AdditionalImages = string.Join(",", dto.AdditionalImages);
        if (dto.StockQuantity.HasValue) product.StockQuantity = dto.StockQuantity.Value;
        if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;
        if (dto.IsFeatured.HasValue) product.IsFeatured = dto.IsFeatured.Value;
        if (dto.IsNewArrival.HasValue) product.IsNewArrival = dto.IsNewArrival.Value;
        if (dto.Tags != null) product.Tags = string.Join(",", dto.Tags);
        
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return MapToDto(product);
    }
    
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Id", "ID sản phẩm không hợp lệ.");
            
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        if (product == null)
            throw NotFoundException.For<Product>(id);
        
        if (product.IsDeleted)
            throw new BusinessRuleException("Sản phẩm đã bị xóa trước đó.");
        
        product.IsDeleted = true;
        product.DeletedAt = DateTime.UtcNow;
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }
    
    public async Task<bool> UpdateStockAsync(Guid id, int quantity, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Id", "ID sản phẩm không hợp lệ.");
            
        if (quantity < 0)
            throw new ValidationException("Quantity", "Số lượng tồn kho không được âm.");
            
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        if (product == null)
            throw NotFoundException.For<Product>(id);
        
        product.StockQuantity = quantity;
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
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
    
    private static ProductListDto MapToListDto(Product product)
    {
        return new ProductListDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Price = product.Price,
            SalePrice = product.SalePrice,
            ImageUrl = product.ImageUrl,
            CategoryName = product.Category?.Name ?? "",
            AgeRange = product.AgeRange,
            AverageRating = product.AverageRating,
            IsInStock = product.IsInStock,
            IsFeatured = product.IsFeatured
        };
    }
    
    #endregion
}
