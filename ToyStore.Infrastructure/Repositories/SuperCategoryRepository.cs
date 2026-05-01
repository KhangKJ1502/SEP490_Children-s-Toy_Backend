using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Repositories;

public class SuperCategoryRepository : ISuperCategoryRepository
{
    private readonly SEP490ToyStoreContext _context;

    public SuperCategoryRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<List<SuperCategory>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SuperCategories
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.SuperCategoryName.Contains(searchTerm));
        }

        query = (sortBy?.Trim().ToLowerInvariant(), sortDesc) switch
        {
            ("name", true) => query.OrderByDescending(x => x.SuperCategoryName),
            ("name", false) => query.OrderBy(x => x.SuperCategoryName),
            ("supercategoryname", true) => query.OrderByDescending(x => x.SuperCategoryName),
            ("supercategoryname", false) => query.OrderBy(x => x.SuperCategoryName),
            ("status", true) => query.OrderByDescending(x => x.IsDeleted),
            ("status", false) => query.OrderBy(x => x.IsDeleted),
            ("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
            ("createdat", false) => query.OrderBy(x => x.CreatedAt),
            (_, true) => query.OrderByDescending(x => x.SuperCategoryId),
            _ => query.OrderBy(x => x.SuperCategoryId)
        };

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SuperCategories
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.SuperCategoryName.Contains(searchTerm));
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string superCategoryName, CancellationToken cancellationToken = default)
    {
        var normalized = superCategoryName.Trim().ToLower();
        return _context.SuperCategories
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AnyAsync(x => x.SuperCategoryName.ToLower() == normalized, cancellationToken);
    }

    public Task<bool> ExistsByNameExceptIdAsync(
        string superCategoryName,
        short superCategoryId,
        CancellationToken cancellationToken = default)
    {
        var normalized = superCategoryName.Trim().ToLower();
        return _context.SuperCategories
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.SuperCategoryId != superCategoryId)
            .AnyAsync(x => x.SuperCategoryName.ToLower() == normalized, cancellationToken);
    }

    public Task<SuperCategory?> GetByIdAsync(short superCategoryId, CancellationToken cancellationToken = default)
    {
        return _context.SuperCategories
            .AsNoTracking()
            .Where(x => x.SuperCategoryId == superCategoryId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SuperCategory> CreateAsync(string superCategoryName, CancellationToken cancellationToken = default)
    {
        var entity = new SuperCategory
        {
            SuperCategoryName = superCategoryName,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.SuperCategories.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task<SuperCategory> UpdateAsync(
        short superCategoryId,
        string superCategoryName,
        bool? isDeleted = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.SuperCategories
            .FirstAsync(x => x.SuperCategoryId == superCategoryId, cancellationToken);

        entity.SuperCategoryName = superCategoryName;
        if (isDeleted.HasValue)
        {
            entity.IsDeleted = isDeleted.Value;
        }
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task UpdateRelatedStatusAsync(
        short superCategoryId,
        bool isDeleted,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        await _context.Categories
            .Where(x => x.SuperCategoryId == superCategoryId)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(x => x.IsDeleted, _ => isDeleted)
                    .SetProperty(x => x.UpdatedAt, _ => now),
                cancellationToken);

        await _context.Products
            .Where(x => x.Category.SuperCategoryId == superCategoryId)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(x => x.IsDeleted, _ => isDeleted)
                    .SetProperty(x => x.ProductStatus, _ => isDeleted ? "Inactive" : "Active")
                    .SetProperty(x => x.UpdatedAt, _ => now),
                cancellationToken);
    }
}