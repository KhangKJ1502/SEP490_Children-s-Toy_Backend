using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly SEP490ToyStoreContext _context;

    public CategoryRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<List<Category>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Category> query = _context.Categories
            .AsNoTracking()
            .Where(x => !x.IsDeleted && !x.SuperCategory.IsDeleted)
            .Include(x => x.SuperCategory);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.CategoryName.Contains(searchTerm) ||
                x.SuperCategory.SuperCategoryName.Contains(searchTerm));
        }

        query = (sortBy?.Trim().ToLowerInvariant(), sortDesc) switch
        {
            ("name", true) => query.OrderByDescending(x => x.CategoryName),
            ("name", false) => query.OrderBy(x => x.CategoryName),
            ("supercategoryname", true) => query.OrderByDescending(x => x.SuperCategory.SuperCategoryName),
            ("supercategoryname", false) => query.OrderBy(x => x.SuperCategory.SuperCategoryName),
            ("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
            ("createdat", false) => query.OrderBy(x => x.CreatedAt),
            (_, true) => query.OrderByDescending(x => x.CategoryId),
            _ => query.OrderBy(x => x.CategoryId)
        };

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Category> query = _context.Categories
            .AsNoTracking()
            .Where(x => !x.IsDeleted && !x.SuperCategory.IsDeleted)
            .Include(x => x.SuperCategory);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.CategoryName.Contains(searchTerm) ||
                x.SuperCategory.SuperCategoryName.Contains(searchTerm));
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string categoryName, CancellationToken cancellationToken = default)
    {
        var normalized = categoryName.Trim().ToLower();
        return _context.Categories
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AnyAsync(x => x.CategoryName.ToLower() == normalized, cancellationToken);
    }

    public Task<bool> ExistsByNameExceptIdAsync(
        string categoryName,
        short categoryId,
        CancellationToken cancellationToken = default)
    {
        var normalized = categoryName.Trim().ToLower();
        return _context.Categories
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.CategoryId != categoryId)
            .AnyAsync(x => x.CategoryName.ToLower() == normalized, cancellationToken);
    }

    public Task<Category?> GetByIdAsync(short categoryId, CancellationToken cancellationToken = default)
    {
        return _context.Categories
            .AsNoTracking()
            .Include(x => x.SuperCategory)
            .Where(x => x.CategoryId == categoryId && !x.IsDeleted && !x.SuperCategory.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Category> CreateAsync(
        short superCategoryId,
        string categoryName,
        CancellationToken cancellationToken = default)
    {
        var entity = new Category
        {
            CategoryName = categoryName,
            SuperCategoryId = superCategoryId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Categories.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _context.Entry(entity)
            .Reference(x => x.SuperCategory)
            .LoadAsync(cancellationToken);

        return entity;
    }

    public async Task<Category> UpdateAsync(
        short categoryId,
        short superCategoryId,
        string categoryName,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Categories
            .Include(x => x.SuperCategory)
            .FirstAsync(x => x.CategoryId == categoryId && !x.IsDeleted, cancellationToken);

        entity.SuperCategoryId = superCategoryId;
        entity.CategoryName = categoryName;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        if (entity.SuperCategory?.SuperCategoryId != superCategoryId)
        {
            await _context.Entry(entity)
                .Reference(x => x.SuperCategory)
                .LoadAsync(cancellationToken);
        }

        return entity;
    }
}