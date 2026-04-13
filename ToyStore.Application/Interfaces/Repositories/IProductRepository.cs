using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Product operations.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all products.
    /// </summary>
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new product.
    /// </summary>
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing product.
    /// </summary>
    void Update(Product product);
    
    /// <summary>
    /// Removes a product.
    /// </summary>
    void Remove(Product product);
    
    /// <summary>
    /// Gets a product by slug.
    /// </summary>
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a product by SKU.
    /// </summary>
    Task<Product?> GetBySKUAsync(string sku, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets products by category.
    /// </summary>
    Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets products by toy category.
    /// </summary>
    Task<IReadOnlyList<Product>> GetByToyCategoryAsync(ToyCategory toyCategory, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets products by age range.
    /// </summary>
    Task<IReadOnlyList<Product>> GetByAgeRangeAsync(AgeRange ageRange, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets featured products.
    /// </summary>
    Task<IReadOnlyList<Product>> GetFeaturedAsync(int limit = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets new arrival products.
    /// </summary>
    Task<IReadOnlyList<Product>> GetNewArrivalsAsync(int limit = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets best-selling products.
    /// </summary>
    Task<IReadOnlyList<Product>> GetBestSellersAsync(int limit = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets products with low stock.
    /// </summary>
    Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches products by term.
    /// </summary>
    Task<IReadOnlyList<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets paginated products with filtering.
    /// </summary>
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? categoryId = null,
        ToyCategory? toyCategory = null,
        AgeRange? ageRange = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default);
}
