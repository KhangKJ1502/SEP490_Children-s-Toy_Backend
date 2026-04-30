using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Common.Models;
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

    public async Task<List<ProductModel>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _context.Products
            .AsNoTracking()
            .Where(x => !x.IsDeleted && !x.Category.IsDeleted && (x.Brand == null || !x.Brand.IsDeleted));

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
            .Select(x => new ProductModel
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                Price = x.Price,
                Quantity = x.Quantity,
                ProductStatus = x.ProductStatus,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.CategoryName,
                BrandId = x.BrandId,
                BrandName = x.Brand != null ? x.Brand.BrandName : null,
                PriceRangeId = x.PriceRangeId,
                MainImageUrl = x.ProductImage != null ? x.ProductImage.ImageUrl : null,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _context.Products
            .AsNoTracking()
            .Where(x => !x.IsDeleted && !x.Category.IsDeleted && (x.Brand == null || !x.Brand.IsDeleted));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.ProductName.Contains(searchTerm) ||
                x.Category.CategoryName.Contains(searchTerm) ||
                (x.Brand != null && x.Brand.BrandName.Contains(searchTerm)));
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<ProductModel?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _context.Products
            .AsNoTracking()
            .Where(x => x.ProductId == productId && !x.IsDeleted && !x.Category.IsDeleted && (x.Brand == null || !x.Brand.IsDeleted))
            .Select(x => new ProductModel
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                Price = x.Price,
                Quantity = x.Quantity,
                ProductStatus = x.ProductStatus,
                LaunchDate = x.LaunchDate,
                StockThreshold = x.StockThreshold,
                LowStockNotificationEnabled = x.LowStockNotificationEnabled,
                LastLowStockNotifiedAt = x.LastLowStockNotifiedAt,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.CategoryName,
                BrandId = x.BrandId,
                BrandName = x.Brand != null ? x.Brand.BrandName : null,
                PriceRangeId = x.PriceRangeId,
                PriceRangeMin = x.PriceRange != null ? x.PriceRange.PriceRangeMin : null,
                PriceRangeMax = x.PriceRange != null ? x.PriceRange.PriceRangeMax : null,
                Description = x.ProductDetail != null ? x.ProductDetail.Description : null,
                MaterialId = x.ProductDetail != null ? x.ProductDetail.MaterialId : null,
                MaterialName = x.ProductDetail != null && x.ProductDetail.Material != null ? x.ProductDetail.Material.MaterialName : null,
                AgeId = x.ProductDetail != null ? x.ProductDetail.AgeId : null,
                AgeRange = x.ProductDetail != null && x.ProductDetail.Age != null ? x.ProductDetail.Age.AgeRange : null,
                SexId = x.ProductDetail != null ? x.ProductDetail.SexId : null,
                SexName = x.ProductDetail != null && x.ProductDetail.Sex != null ? x.ProductDetail.Sex.SexName : null,
                OriginId = x.ProductDetail != null ? x.ProductDetail.OriginId : null,
                OriginName = x.ProductDetail != null && x.ProductDetail.Origin != null ? x.ProductDetail.Origin.OriginName : null,
                MainImageUrl = x.ProductImage != null ? x.ProductImage.ImageUrl : null,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductModel> CreateAsync(ProductCreateModel model, CancellationToken cancellationToken = default)
    {
        var entity = new Product
        {
            CategoryId = model.CategoryId,
            BrandId = model.BrandId,
            PriceRangeId = model.PriceRangeId,
            ProductName = model.ProductName,
            Price = model.Price,
            Quantity = model.Quantity,
            ProductStatus = model.ProductStatus,
            LaunchDate = model.LaunchDate,
            StockThreshold = model.StockThreshold,
            LowStockNotificationEnabled = model.LowStockNotificationEnabled,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        if (model.HasDetail)
        {
            entity.ProductDetail = new ProductDetail
            {
                Description = model.Description,
                MaterialId = model.MaterialId,
                AgeId = model.AgeId,
                SexId = model.SexId,
                OriginId = model.OriginId
            };
        }

        if (!string.IsNullOrWhiteSpace(model.MainImageUrl))
        {
            entity.ProductImage = new ProductImage
            {
                ImageUrl = model.MainImageUrl,
                IsMain = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        await _context.Products.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var created = await GetByIdAsync(entity.ProductId, cancellationToken);
        return created!;
    }

    public async Task<ProductModel> UpdateAsync(
        int productId,
        ProductUpdateModel model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Products
            .Include(x => x.ProductDetail)
            .Include(x => x.ProductImage)
            .FirstAsync(x => x.ProductId == productId && !x.IsDeleted, cancellationToken);

        if (model.CategoryId.HasValue)
        {
            entity.CategoryId = model.CategoryId.Value;
        }

        if (model.BrandId.HasValue)
        {
            entity.BrandId = model.BrandId;
        }

        if (model.PriceRangeId.HasValue)
        {
            entity.PriceRangeId = model.PriceRangeId;
        }

        if (!string.IsNullOrWhiteSpace(model.ProductName))
        {
            entity.ProductName = model.ProductName;
        }

        if (model.Price.HasValue)
        {
            entity.Price = model.Price.Value;
        }

        if (model.Quantity.HasValue)
        {
            entity.Quantity = model.Quantity.Value;
        }

        if (!string.IsNullOrWhiteSpace(model.ProductStatus))
        {
            entity.ProductStatus = model.ProductStatus;
        }

        if (model.LaunchDate.HasValue)
        {
            entity.LaunchDate = model.LaunchDate;
        }

        if (model.StockThreshold.HasValue)
        {
            entity.StockThreshold = model.StockThreshold.Value;
        }

        if (model.LowStockNotificationEnabled.HasValue)
        {
            entity.LowStockNotificationEnabled = model.LowStockNotificationEnabled.Value;
        }

        if (model.HasDetail)
        {
            if (entity.ProductDetail == null)
            {
                entity.ProductDetail = new ProductDetail();
            }

            if (model.Description != null)
            {
                entity.ProductDetail.Description = model.Description;
            }

            if (model.MaterialId.HasValue)
            {
                entity.ProductDetail.MaterialId = model.MaterialId;
            }

            if (model.AgeId.HasValue)
            {
                entity.ProductDetail.AgeId = model.AgeId;
            }

            if (model.SexId.HasValue)
            {
                entity.ProductDetail.SexId = model.SexId;
            }

            if (model.OriginId.HasValue)
            {
                entity.ProductDetail.OriginId = model.OriginId;
            }
        }

        if (!string.IsNullOrWhiteSpace(model.MainImageUrl))
        {
            if (entity.ProductImage == null)
            {
                entity.ProductImage = new ProductImage
                {
                    ImageUrl = model.MainImageUrl,
                    IsMain = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                entity.ProductImage.ImageUrl = model.MainImageUrl;
                entity.ProductImage.UpdatedAt = DateTime.UtcNow;
            }
        }

        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var updated = await GetByIdAsync(entity.ProductId, cancellationToken);
        return updated!;
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
