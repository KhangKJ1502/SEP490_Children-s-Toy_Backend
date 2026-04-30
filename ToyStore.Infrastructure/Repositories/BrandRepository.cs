using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Common.Models;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly SEP490ToyStoreContext _context;

    public BrandRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<List<BrandModel>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Brands
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

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
            (_, true) => query.OrderByDescending(x => x.BrandId),
            _ => query.OrderBy(x => x.BrandId)
        };

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BrandModel
            {
                BrandId = x.BrandId,
                BrandName = x.BrandName,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Brands
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

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
            .Where(x => !x.IsDeleted)
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
            .Where(x => !x.IsDeleted && x.BrandId != brandId)
            .AnyAsync(x => x.BrandName.ToLower() == normalized, cancellationToken);
    }

    public Task<BrandModel?> GetByIdAsync(short brandId, CancellationToken cancellationToken = default)
    {
        return _context.Brands
            .AsNoTracking()
            .Where(x => x.BrandId == brandId && !x.IsDeleted)
            .Select(x => new BrandModel
            {
                BrandId = x.BrandId,
                BrandName = x.BrandName,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BrandModel> CreateAsync(string brandName, CancellationToken cancellationToken = default)
    {
        var entity = new Brand
        {
            BrandName = brandName,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Brands.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new BrandModel
        {
            BrandId = entity.BrandId,
            BrandName = entity.BrandName,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<BrandModel> UpdateAsync(
        short brandId,
        string brandName,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Brands
            .FirstAsync(x => x.BrandId == brandId && !x.IsDeleted, cancellationToken);

        entity.BrandName = brandName;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new BrandModel
        {
            BrandId = entity.BrandId,
            BrandName = entity.BrandName,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
