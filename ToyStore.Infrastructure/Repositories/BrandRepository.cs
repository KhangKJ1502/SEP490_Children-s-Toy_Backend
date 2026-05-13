using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly SEP490ToyStoreContext _context;

    public BrandRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<List<Brand>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Brands.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.BrandName.Contains(searchTerm));
        }

        query = (sortBy?.Trim().ToLowerInvariant(), sortDesc) switch
        {
            ("name", true) => query.OrderByDescending(x => x.BrandName),
            ("name", false) => query.OrderBy(x => x.BrandName),
            ("brandname", true) => query.OrderByDescending(x => x.BrandName),
            ("brandname", false) => query.OrderBy(x => x.BrandName),
            ("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
            ("createdat", false) => query.OrderBy(x => x.CreatedAt),
            ("updatedat", true) => query.OrderByDescending(x => x.UpdatedAt),
            ("updatedat", false) => query.OrderBy(x => x.UpdatedAt),
            ("status", true) => query.OrderByDescending(x => x.IsDeleted),
            ("status", false) => query.OrderBy(x => x.IsDeleted),
            (_, true) => query.OrderByDescending(x => x.BrandId),
            _ => query.OrderBy(x => x.BrandId)
        };

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Brands.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.BrandName.Contains(searchTerm));
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string brandName, CancellationToken cancellationToken = default)
    {
        var normalized = brandName.Trim().ToLower();
        return _context.Brands
            .AsNoTracking()
            .AnyAsync(x => x.BrandName.ToLower() == normalized, cancellationToken);
    }

    public Task<bool> ExistsByNameExceptIdAsync(
        string brandName,
        short brandId,
        CancellationToken cancellationToken = default)
    {
        var normalized = brandName.Trim().ToLower();
        return _context.Brands
            .AsNoTracking()
            .Where(x => x.BrandId != brandId)
            .AnyAsync(x => x.BrandName.ToLower() == normalized, cancellationToken);
    }

    public Task<Brand?> GetByIdAsync(short brandId, CancellationToken cancellationToken = default)
    {
        return _context.Brands
            .AsNoTracking()
            .Where(x => x.BrandId == brandId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Brand> CreateAsync(string brandName, CancellationToken cancellationToken = default)
    {
        var entity = new Brand
        {
            BrandName = brandName,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Brands.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task<Brand> UpdateAsync(
        short brandId,
        string brandName,
        bool isDeleted,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Brands
            .FirstAsync(x => x.BrandId == brandId, cancellationToken);

        var hasNameChanged = !string.Equals(entity.BrandName, brandName, StringComparison.Ordinal);
        var hasStatusChanged = entity.IsDeleted != isDeleted;

        if (hasNameChanged || hasStatusChanged)
        {
            entity.BrandName = brandName;
            entity.IsDeleted = isDeleted;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public Task<List<Brand>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _context.Brands
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateRelatedProductStatusAsync(
        short brandId,
        bool isDeleted,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        await _context.Products
            .Where(x => x.BrandId == brandId)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(x => x.ProductStatus, _ => isDeleted ? "Inactive" : "Active")
                    .SetProperty(x => x.UpdatedAt, _ => now),
                cancellationToken);
    }
}
