using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Product repository implementation.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ToyStoreDbContext _context;
    private readonly DbSet<Product> _dbSet;
    
    public ProductRepository(ToyStoreDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Product>();
    }
    
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }
    
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => !p.IsDeleted)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(product, cancellationToken);
        return product;
    }
    
    public void Update(Product product)
    {
        _dbSet.Update(product);
    }
    
    public void Remove(Product product)
    {
        _dbSet.Remove(product);
    }
    
    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == slug && !p.IsDeleted, cancellationToken);
    }
    
    public async Task<Product?> GetBySKUAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.SKU == sku && !p.IsDeleted, cancellationToken);
    }
    
    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(
        Guid categoryId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId && p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Product>> GetByToyCategoryAsync(
        ToyCategory toyCategory, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Where(p => p.ToyCategory == toyCategory && p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Product>> GetByAgeRangeAsync(
        AgeRange ageRange, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Where(p => p.AgeRange == ageRange && p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Product>> GetFeaturedAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Where(p => p.IsFeatured && p.IsActive && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Product>> GetNewArrivalsAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Where(p => p.IsNewArrival && p.IsActive && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Product>> GetBestSellersAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderByDescending(p => p.PurchaseCount)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.StockQuantity <= p.LowStockThreshold && p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Product>> SearchAsync(
        string searchTerm, 
        CancellationToken cancellationToken = default)
    {
        var term = searchTerm.ToLower();
        return await _dbSet
            .Include(p => p.Category)
            .Where(p => p.IsActive && !p.IsDeleted && (
                p.Name.ToLower().Contains(term) ||
                p.Description!.ToLower().Contains(term) ||
                p.Tags!.ToLower().Contains(term) ||
                p.Brand!.ToLower().Contains(term)))
            .OrderByDescending(p => p.ViewCount)
            .Take(50)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(p => p.Category).Where(p => p.IsActive && !p.IsDeleted);
        
        // Apply filters
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);
            
        if (toyCategory.HasValue)
            query = query.Where(p => p.ToyCategory == toyCategory.Value);
            
        if (ageRange.HasValue)
            query = query.Where(p => p.AgeRange == ageRange.Value);
            
        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);
            
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);
            
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p => 
                p.Name.ToLower().Contains(term) ||
                p.Description!.ToLower().Contains(term));
        }
        
        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);
        
        // Apply sorting
        query = sortBy?.ToLower() switch
        {
            "price" => sortDescending 
                ? query.OrderByDescending(p => p.Price) 
                : query.OrderBy(p => p.Price),
            "name" => sortDescending 
                ? query.OrderByDescending(p => p.Name) 
                : query.OrderBy(p => p.Name),
            "rating" => query.OrderByDescending(p => p.AverageRating),
            "popularity" => query.OrderByDescending(p => p.PurchaseCount),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };
        
        // Apply pagination
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return (items, totalCount);
    }
}
