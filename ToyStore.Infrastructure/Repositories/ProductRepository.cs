using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly SEP490ToyStoreContext _context;

    public ProductRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _context.Products
            .AsNoTrackingWithIdentityResolution()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.ProductImage)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.ProductName.Contains(searchTerm) ||
                x.Category.CategoryName.Contains(searchTerm) ||
                (x.Brand != null && x.Brand.BrandName.Contains(searchTerm)));
        }

        query = (sortBy?.Trim().ToLowerInvariant(), sortDesc) switch
        {
            ("name", true) => query.OrderByDescending(x => x.ProductName),
            ("name", false) => query.OrderBy(x => x.ProductName),
            ("price", true) => query.OrderByDescending(x => x.Price),
            ("price", false) => query.OrderBy(x => x.Price),
            ("quantity", true) => query.OrderByDescending(x => x.Quantity),
            ("quantity", false) => query.OrderBy(x => x.Quantity),
            ("status", true) => query.OrderByDescending(x => x.ProductStatus),
            ("status", false) => query.OrderBy(x => x.ProductStatus),
            ("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
            ("createdat", false) => query.OrderBy(x => x.CreatedAt),
            (_, true) => query.OrderByDescending(x => x.ProductId),
            _ => query.OrderBy(x => x.ProductId)
        };

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _context.Products
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.ProductName.Contains(searchTerm) ||
                x.Category.CategoryName.Contains(searchTerm) ||
                (x.Brand != null && x.Brand.BrandName.Contains(searchTerm)));
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.PriceRange)
            .Include(x => x.ProductDetail)
                .ThenInclude(d => d!.Material)
            .Include(x => x.ProductDetail)
                .ThenInclude(d => d!.Age)
            .Include(x => x.ProductDetail)
                .ThenInclude(d => d!.Sex)
            .Include(x => x.ProductDetail)
                .ThenInclude(d => d!.Origin)
            .Include(x => x.ProductImage)
            .Where(x => x.ProductId == productId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Product> CreateAsync(
        Product product,
        IReadOnlyCollection<string>? additionalImageUrls = null,
        CancellationToken cancellationToken = default)
    {
        product.IsDeleted = false;
        product.CreatedAt = DateTime.UtcNow;

        if (product.ProductImage != null)
        {
            product.ProductImage.IsMain = true;
            product.ProductImage.CreatedAt = DateTime.UtcNow;
        }

        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await SaveAdditionalImagesAsync(product.ProductId, additionalImageUrls, cancellationToken);

        var created = await GetByIdAsync(product.ProductId, cancellationToken);
        return created!;
    }

    public async Task<Product> UpdateAsync(
        Product product,
        IReadOnlyCollection<string>? additionalImageUrls = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.Products
            .Include(x => x.ProductDetail)
            .Include(x => x.ProductImage)
            .FirstAsync(x => x.ProductId == product.ProductId, cancellationToken);

        existing.CategoryId = product.CategoryId;
        existing.BrandId = product.BrandId;
        existing.PriceRangeId = product.PriceRangeId;
        existing.ProductName = product.ProductName;
        existing.Price = product.Price;
        existing.Quantity = product.Quantity;
        existing.ProductStatus = product.ProductStatus;
        existing.LaunchDate = product.LaunchDate;
        existing.StockThreshold = product.StockThreshold;
        existing.LowStockNotificationEnabled = product.LowStockNotificationEnabled;
        existing.UpdatedAt = DateTime.UtcNow;

        if (product.ProductDetail != null)
        {
            if (existing.ProductDetail == null)
            {
                existing.ProductDetail = new ProductDetail
                {
                    ProductId = existing.ProductId
                };
            }

            existing.ProductDetail.Description = product.ProductDetail.Description;
            existing.ProductDetail.MaterialId = product.ProductDetail.MaterialId;
            existing.ProductDetail.AgeId = product.ProductDetail.AgeId;
            existing.ProductDetail.SexId = product.ProductDetail.SexId;
            existing.ProductDetail.OriginId = product.ProductDetail.OriginId;
        }

        if (product.ProductImage != null)
        {
            if (existing.ProductImage == null)
            {
                existing.ProductImage = new ProductImage
                {
                    ProductId = existing.ProductId,
                    ImageUrl = product.ProductImage.ImageUrl,
                    IsMain = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                existing.ProductImage.ImageUrl = product.ProductImage.ImageUrl;
                existing.ProductImage.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (additionalImageUrls != null)
        {
            await _context.ProductImages
                .IgnoreQueryFilters()
                .Where(x => x.ProductId == existing.ProductId && !x.IsMain)
                .ExecuteDeleteAsync(cancellationToken);

            await SaveAdditionalImagesAsync(existing.ProductId, additionalImageUrls, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var updated = await GetByIdAsync(existing.ProductId, cancellationToken);
        return updated!;
    }

    public Task<List<string>> GetAdditionalImageUrlsAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        return _context.ProductImages
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.ProductId == productId && !x.IsMain)
            .OrderBy(x => x.ImageId)
            .Select(x => x.ImageUrl)
            .ToListAsync(cancellationToken);
    }

    private async Task SaveAdditionalImagesAsync(
        int productId,
        IReadOnlyCollection<string>? additionalImageUrls,
        CancellationToken cancellationToken)
    {
        if (additionalImageUrls is not { Count: > 0 })
        {
            return;
        }

        var sanitizedUrls = additionalImageUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct()
            .ToList();

        if (sanitizedUrls.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var url in sanitizedUrls)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"INSERT INTO [ProductImages] ([ProductID], [ImageUrl], [IsMain], [CreatedAt], [UpdatedAt])
                   VALUES ({productId}, {url}, {false}, {now}, {null})",
                cancellationToken);
        }
    }

    public Task<bool> CategoryExistsAsync(short categoryId, CancellationToken cancellationToken = default)
    {
        return _context.Categories
            .AsNoTracking()
            .AnyAsync(x => x.CategoryId == categoryId && !x.IsDeleted, cancellationToken);
    }

    public Task<bool> BrandExistsAsync(short brandId, CancellationToken cancellationToken = default)
    {
        return _context.Brands
            .AsNoTracking()
            .AnyAsync(x => x.BrandId == brandId && !x.IsDeleted, cancellationToken);
    }

    public Task<bool> PriceRangeExistsAsync(byte priceRangeId, CancellationToken cancellationToken = default)
    {
        return _context.PriceRanges
            .AsNoTracking()
            .AnyAsync(x => x.PriceRangeId == priceRangeId, cancellationToken);
    }

    public Task<bool> MaterialExistsAsync(short materialId, CancellationToken cancellationToken = default)
    {
        return _context.Materials
            .AsNoTracking()
            .AnyAsync(x => x.MaterialId == materialId && !x.IsDeleted, cancellationToken);
    }

    public Task<bool> AgeExistsAsync(byte ageId, CancellationToken cancellationToken = default)
    {
        return _context.Ages
            .AsNoTracking()
            .AnyAsync(x => x.AgeId == ageId, cancellationToken);
    }

    public Task<bool> SexExistsAsync(byte sexId, CancellationToken cancellationToken = default)
    {
        return _context.Sexes
            .AsNoTracking()
            .AnyAsync(x => x.SexId == sexId, cancellationToken);
    }

    public Task<bool> OriginExistsAsync(byte originId, CancellationToken cancellationToken = default)
    {
        return _context.Origins
            .AsNoTracking()
            .AnyAsync(x => x.OriginId == originId, cancellationToken);
    }
}
