using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly SEP490ToyStoreContext _context;
    private readonly ITimeProvider _timeProvider;

    public ProductRepository(SEP490ToyStoreContext context, ITimeProvider timeProvider)
    {
        _context      = context;
        _timeProvider = timeProvider;
    }

    public async Task<List<Product>> GetPagedAsync(
        int pageNumber,
        int pageSize,
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
        IQueryable<Product> query = _context.Products
            .AsNoTrackingWithIdentityResolution()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.ProductDetail)
            .Include(x => x.ProductImage)
            .Include(x => x.ProductPromotions)
                .ThenInclude(pp => pp.Promotion)
                    .ThenInclude(p => p.PromotionTimeSlots)
            .Include(x => x.PromotionProductSlots)
                .ThenInclude(pps => pps.TimeSlot)
                    .ThenInclude(ts => ts.Promotion)
            .AsQueryable();

        if (superCategoryId.HasValue)
        {
            query = query.Where(x => x.Category.SuperCategoryId == superCategoryId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (categoryIds is { Count: > 0 })
        {
            query = query.Where(x => categoryIds.Contains(x.CategoryId));
        }

        if (brandIds is { Count: > 0 })
        {
            query = query.Where(x => x.BrandId.HasValue && brandIds.Contains(x.BrandId.Value));
        }

        if (priceRangeIds is { Count: > 0 })
        {
            query = query.Where(x => x.PriceRangeId.HasValue && priceRangeIds.Contains(x.PriceRangeId.Value));
        }

        // Filter theo giá hiển thị thực tế (giá sale nếu có promotion, không thì giá gốc)
        if (minPrice.HasValue)
        {
            query = query.Where(x =>
                // Giá gốc >= minPrice
                x.Price >= minPrice.Value
                // HOẶC có promotion active với SalePrice >= minPrice
                || x.ProductPromotions.Any(pp =>
                    pp.IsActive && pp.SalePrice >= minPrice.Value));
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x =>
                // Nếu có promotion active thì dùng SalePrice
                (x.ProductPromotions.Any(pp => pp.IsActive)
                    && x.ProductPromotions.Where(pp => pp.IsActive).Min(pp => pp.SalePrice) <= maxPrice.Value)
                // HOẶC không có promotion thì dùng Price
                || (!x.ProductPromotions.Any(pp => pp.IsActive)
                    && x.Price <= maxPrice.Value));
        }

        if (materialIds is { Count: > 0 })
        {
            query = query.Where(x => x.ProductDetail != null
                && x.ProductDetail.MaterialId.HasValue
                && materialIds.Contains(x.ProductDetail.MaterialId.Value));
        }

        if (ageIds is { Count: > 0 })
        {
            query = query.Where(x => x.ProductDetail != null
                && x.ProductDetail.AgeId.HasValue
                && ageIds.Contains(x.ProductDetail.AgeId.Value));
        }

        if (sexIds is { Count: > 0 })
        {
            query = query.Where(x => x.ProductDetail != null
                && x.ProductDetail.SexId.HasValue
                && sexIds.Contains(x.ProductDetail.SexId.Value));
        }

        if (originIds is { Count: > 0 })
        {
            query = query.Where(x => x.ProductDetail != null
                && x.ProductDetail.OriginId.HasValue
                && originIds.Contains(x.ProductDetail.OriginId.Value));
        }

        if (rating.HasValue)
        {
            query = query.Where(x =>
                _context.ReviewProducts
                    .Where(r => r.ProductId == x.ProductId && !r.IsDeleted && r.ModerationStatus == "Approved")
                    .Select(r => (double?)r.Rating)
                    .Average() != null
                && Math.Round(
                    _context.ReviewProducts
                        .Where(r => r.ProductId == x.ProductId && !r.IsDeleted && r.ModerationStatus == "Approved")
                        .Select(r => (double?)r.Rating)
                        .Average()!.Value) == rating.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(status))
        {
            var statuses = status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (statuses.Length > 1)
            {
                query = query.Where(x => statuses.Contains(x.ProductStatus));
            }
            else
            {
                query = query.Where(x => x.ProductStatus == status);
            }
        }

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
            ("productname", true) => query.OrderByDescending(x => x.ProductName),
            ("productname", false) => query.OrderBy(x => x.ProductName),
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

    public Task<int> CountAsync(
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
        IQueryable<Product> query = _context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.ProductDetail)
            .AsQueryable();

        if (superCategoryId.HasValue)
        {
            query = query.Where(x => x.Category.SuperCategoryId == superCategoryId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (categoryIds is { Count: > 0 })
        {
            query = query.Where(x => categoryIds.Contains(x.CategoryId));
        }

        if (brandIds is { Count: > 0 })
        {
            query = query.Where(x => x.BrandId.HasValue && brandIds.Contains(x.BrandId.Value));
        }

        if (priceRangeIds is { Count: > 0 })
        {
            query = query.Where(x => x.PriceRangeId.HasValue && priceRangeIds.Contains(x.PriceRangeId.Value));
        }

        // Filter theo giá hiển thị thực tế (giá sale nếu có promotion, không thì giá gốc)
        if (minPrice.HasValue)
        {
            query = query.Where(x =>
                // Giá gốc >= minPrice
                x.Price >= minPrice.Value
                // HOẶC có promotion active với SalePrice >= minPrice
                || x.ProductPromotions.Any(pp =>
                    pp.IsActive && pp.SalePrice >= minPrice.Value));
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x =>
                // Nếu có promotion active thì dùng SalePrice
                (x.ProductPromotions.Any(pp => pp.IsActive)
                    && x.ProductPromotions.Where(pp => pp.IsActive).Min(pp => pp.SalePrice) <= maxPrice.Value)
                // HOẶC không có promotion thì dùng Price
                || (!x.ProductPromotions.Any(pp => pp.IsActive)
                    && x.Price <= maxPrice.Value));
        }

        if (materialIds is { Count: > 0 })
        {
            query = query.Where(x => x.ProductDetail != null
                && x.ProductDetail.MaterialId.HasValue
                && materialIds.Contains(x.ProductDetail.MaterialId.Value));
        }

        if (ageIds is { Count: > 0 })
        {
            query = query.Where(x => x.ProductDetail != null
                && x.ProductDetail.AgeId.HasValue
                && ageIds.Contains(x.ProductDetail.AgeId.Value));
        }

        if (sexIds is { Count: > 0 })
        {
            query = query.Where(x => x.ProductDetail != null
                && x.ProductDetail.SexId.HasValue
                && sexIds.Contains(x.ProductDetail.SexId.Value));
        }

        if (originIds is { Count: > 0 })
        {
            query = query.Where(x => x.ProductDetail != null
                && x.ProductDetail.OriginId.HasValue
                && originIds.Contains(x.ProductDetail.OriginId.Value));
        }

        if (rating.HasValue)
        {
            query = query.Where(x =>
                _context.ReviewProducts
                    .Where(r => r.ProductId == x.ProductId && !r.IsDeleted && r.ModerationStatus == "Approved")
                    .Select(r => (double?)r.Rating)
                    .Average() != null
                && Math.Round(
                    _context.ReviewProducts
                        .Where(r => r.ProductId == x.ProductId && !r.IsDeleted && r.ModerationStatus == "Approved")
                        .Select(r => (double?)r.Rating)
                        .Average()!.Value) == rating.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(status))
        {
            var statuses = status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (statuses.Length > 1)
            {
                query = query.Where(x => statuses.Contains(x.ProductStatus));
            }
            else
            {
                query = query.Where(x => x.ProductStatus == status);
            }
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.ProductName.Contains(searchTerm) ||
                x.Category.CategoryName.Contains(searchTerm) ||
                (x.Brand != null && x.Brand.BrandName.Contains(searchTerm)));
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task<List<Product>> GetInventoryReportAsync(
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        short? categoryId = null,
        int? brandId = null,
        byte? priceRangeId = null,
        short? materialId = null,
        byte? ageId = null,
        byte? originId = null,
        string? status = null,
        bool lowStockOnly = false,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? dateField = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _context.Products
            .AsNoTrackingWithIdentityResolution()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.ProductDetail)
                .ThenInclude(d => d!.Material)
            .Include(x => x.ProductDetail)
                .ThenInclude(d => d!.Age)
            .Include(x => x.ProductDetail)
                .ThenInclude(d => d!.Origin)
            .Include(x => x.ProductPromotions)
                .ThenInclude(pp => pp.Promotion)
                    .ThenInclude(p => p.PromotionTimeSlots)
            .Include(x => x.PromotionProductSlots)
                .ThenInclude(pps => pps.TimeSlot)
                    .ThenInclude(ts => ts.Promotion)
            .Include(x => x.ReviewProducts)
            .Include(x => x.OrderDetails)
                .ThenInclude(od => od.Order)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (brandId.HasValue)
        {
            query = query.Where(x => x.BrandId.HasValue && x.BrandId.Value == brandId.Value);
        }

        if (priceRangeId.HasValue)
        {
            query = query.Where(x => x.PriceRangeId.HasValue && x.PriceRangeId.Value == priceRangeId.Value);
        }

        if (materialId.HasValue)
        {
            query = query.Where(x => x.ProductDetail != null
                && x.ProductDetail.MaterialId.HasValue
                && x.ProductDetail.MaterialId.Value == materialId.Value);
        }

        if (ageId.HasValue)
        {
            query = query.Where(x => x.ProductDetail != null
                && x.ProductDetail.AgeId.HasValue
                && x.ProductDetail.AgeId.Value == ageId.Value);
        }

        if (originId.HasValue)
        {
            query = query.Where(x => x.ProductDetail != null
                && x.ProductDetail.OriginId.HasValue
                && x.ProductDetail.OriginId.Value == originId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statuses = status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (statuses.Length > 1)
            {
                query = query.Where(x => statuses.Contains(x.ProductStatus));
            }
            else
            {
                query = query.Where(x => x.ProductStatus == status);
            }
        }

        if (lowStockOnly)
        {
            query = query.Where(x => x.Quantity <= x.StockThreshold);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.ProductName.Contains(searchTerm) ||
                x.Category.CategoryName.Contains(searchTerm) ||
                (x.Brand != null && x.Brand.BrandName.Contains(searchTerm)));
        }

        if (dateFrom.HasValue || dateTo.HasValue)
        {
            var field = dateField?.Trim().ToLowerInvariant();
            var from = dateFrom;
            var to = dateTo;

            if (string.Equals(field, "updatedat", StringComparison.OrdinalIgnoreCase))
            {
                if (from.HasValue)
                {
                    query = query.Where(x => x.UpdatedAt.HasValue && x.UpdatedAt.Value >= from.Value);
                }

                if (to.HasValue)
                {
                    query = query.Where(x => x.UpdatedAt.HasValue && x.UpdatedAt.Value <= to.Value);
                }
            }
            else
            {
                if (from.HasValue)
                {
                    query = query.Where(x => x.CreatedAt >= from.Value);
                }

                if (to.HasValue)
                {
                    query = query.Where(x => x.CreatedAt <= to.Value);
                }
            }
        }

        query = (sortBy?.Trim().ToLowerInvariant(), sortDesc) switch
        {
            ("name", true) => query.OrderByDescending(x => x.ProductName),
            ("name", false) => query.OrderBy(x => x.ProductName),
            ("productname", true) => query.OrderByDescending(x => x.ProductName),
            ("productname", false) => query.OrderBy(x => x.ProductName),
            ("price", true) => query.OrderByDescending(x => x.Price),
            ("price", false) => query.OrderBy(x => x.Price),
            ("quantity", true) => query.OrderByDescending(x => x.Quantity),
            ("quantity", false) => query.OrderBy(x => x.Quantity),
            ("status", true) => query.OrderByDescending(x => x.ProductStatus),
            ("status", false) => query.OrderBy(x => x.ProductStatus),
            ("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
            ("createdat", false) => query.OrderBy(x => x.CreatedAt),
            ("updatedat", true) => query.OrderByDescending(x => x.UpdatedAt),
            ("updatedat", false) => query.OrderBy(x => x.UpdatedAt),
            (_, true) => query.OrderByDescending(x => x.ProductId),
            _ => query.OrderBy(x => x.ProductId)
        };

        return await query.ToListAsync(cancellationToken);
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
            .Include(x => x.ProductPromotions)
                .ThenInclude(pp => pp.Promotion)
                    .ThenInclude(p => p.PromotionTimeSlots)
            .Include(x => x.PromotionProductSlots)
                .ThenInclude(pps => pps.TimeSlot)
                    .ThenInclude(ts => ts.Promotion)
            .Include(x => x.ReviewProducts)
            .Include(x => x.OrderDetails)
                .ThenInclude(od => od.Order)
            .Where(x => x.ProductId == productId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Product> CreateAsync(
        Product product,
        IReadOnlyCollection<string>? additionalImageUrls = null,
        CancellationToken cancellationToken = default)
    {
        product.IsDeleted = false;
        product.CreatedAt = _timeProvider.UtcNow;

        if (product.ProductImage != null)
        {
            product.ProductImage.IsMain = true;
            product.ProductImage.CreatedAt = _timeProvider.UtcNow;
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
        existing.UpdatedAt = _timeProvider.UtcNow;

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
                    CreatedAt = _timeProvider.UtcNow
                };
            }
            else
            {
                existing.ProductImage.ImageUrl = product.ProductImage.ImageUrl;
                existing.ProductImage.UpdatedAt = _timeProvider.UtcNow;
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

    public void Update(Product product)
    {
        _context.Products.Update(product);
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

        var now = _timeProvider.UtcNow;
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

    public Task<List<Product>> GetByIdsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default)
    {
        return _context.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId) && !x.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public Task<List<PriceRange>> GetPriceRangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.PriceRanges
            .AsNoTracking()
            .OrderBy(x => x.PriceRangeMin)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Material>> GetMaterialsAsync(CancellationToken cancellationToken = default)
    {
        return _context.Materials
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.MaterialName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Age>> GetAgesAsync(CancellationToken cancellationToken = default)
    {
        return _context.Ages
            .AsNoTracking()
            .OrderBy(x => x.AgeRange)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Sex>> GetSexesAsync(CancellationToken cancellationToken = default)
    {
        return _context.Sexes
            .AsNoTracking()
            .OrderBy(x => x.SexName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Origin>> GetOriginsAsync(CancellationToken cancellationToken = default)
    {
        return _context.Origins
            .AsNoTracking()
            .OrderBy(x => x.OriginName)
            .ToListAsync(cancellationToken);
    }

    public Task AdjustStockAsync(int productId, int amount, CancellationToken cancellationToken = default)
    {
        return _context.Database.ExecuteSqlRawAsync(
            "UPDATE Products SET Quantity = Quantity + {0} WHERE ProductID = {1}",
            new object[] { amount, productId },
            cancellationToken);
    }
}
