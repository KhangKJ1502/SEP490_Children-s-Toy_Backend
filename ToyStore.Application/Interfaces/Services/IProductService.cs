using ToyStore.Application.DTOs;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service interface for product operations.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a product by slug.
    /// </summary>
    Task<ProductDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets paginated products with filtering.
    /// </summary>
    Task<PaginatedResponse<ProductListDto>> GetProductsAsync(
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
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets featured products.
    /// </summary>
    Task<IReadOnlyList<ProductListDto>> GetFeaturedProductsAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets new arrival products.
    /// </summary>
    Task<IReadOnlyList<ProductListDto>> GetNewArrivalsAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new product.
    /// </summary>
    Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing product.
    /// </summary>
    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a product (soft delete).
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates product stock quantity.
    /// </summary>
    Task<bool> UpdateStockAsync(Guid id, int quantity, CancellationToken cancellationToken = default);
}
