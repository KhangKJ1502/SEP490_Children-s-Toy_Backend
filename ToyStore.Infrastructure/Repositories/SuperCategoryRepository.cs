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
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.SuperCategoryName.Contains(searchTerm));
        }

        query = (sortBy?.Trim().ToLowerInvariant(), sortDesc) switch
        {
            ("name", true) => query.OrderByDescending(x => x.SuperCategoryName),
            ("name", false) => query.OrderBy(x => x.SuperCategoryName),
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
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

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
            .Where(x => x.SuperCategoryId == superCategoryId && !x.IsDeleted)
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
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.SuperCategories
            .FirstAsync(x => x.SuperCategoryId == superCategoryId && !x.IsDeleted, cancellationToken);

        entity.SuperCategoryName = superCategoryName;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }
}