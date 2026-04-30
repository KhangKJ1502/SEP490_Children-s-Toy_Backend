using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class BlogRepository : IBlogRepository
{
    private readonly SEP490ToyStoreContext _context;

    public BlogRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<List<BlogPost>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        int? createdByAccountId = null,
        bool onlyPublished = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(searchTerm, status, createdByAccountId, onlyPublished);
        query = ApplySorting(query, sortBy, sortDesc);

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        string? searchTerm = null,
        string? status = null,
        int? createdByAccountId = null,
        bool onlyPublished = false,
        CancellationToken cancellationToken = default)
    {
        return BuildQuery(searchTerm, status, createdByAccountId, onlyPublished).CountAsync(cancellationToken);
    }

    public Task<BlogPost?> GetByIdAsync(int blogPostId, CancellationToken cancellationToken = default)
    {
        return _context.BlogPosts
            .Include(x => x.Account)
            .Include(x => x.ApprovedByNavigation)
            .FirstOrDefaultAsync(x => x.BlogPostId == blogPostId && !x.IsDeleted, cancellationToken);
    }

    public Task<bool> BlogCategoryExistsAsync(short blogCategoryId, CancellationToken cancellationToken = default)
    {
        return _context.BlogCategories
            .AsNoTracking()
            .AnyAsync(x => x.BlogCategoryId == blogCategoryId, cancellationToken);
    }

    public async Task<BlogPost> CreateAsync(BlogPost entity, CancellationToken cancellationToken = default)
    {
        await _context.BlogPosts.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<BlogPost> UpdateAsync(BlogPost entity, CancellationToken cancellationToken = default)
    {
        _context.BlogPosts.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<int> DemoteOldestFeaturedAsync(CancellationToken cancellationToken = default)
    {
        var featuredCount = await _context.BlogPosts
            .CountAsync(x => !x.IsDeleted && x.IsFeatured, cancellationToken);

        if (featuredCount <= 5)
        {
            return 0;
        }

        var oldestFeatured = await _context.BlogPosts
            .Where(x => !x.IsDeleted && x.IsFeatured)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.BlogPostId)
            .FirstOrDefaultAsync(cancellationToken);

        if (oldestFeatured == null)
        {
            return 0;
        }

        oldestFeatured.IsFeatured = false;
        oldestFeatured.UpdatedAt = DateTime.UtcNow;
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> PublishDueScheduledBlogsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var dueBlogs = await _context.BlogPosts
            .Where(x => !x.IsDeleted
                && x.Status == "Scheduled"
                && x.BlogAt != null
                && x.BlogAt <= utcNow)
            .ToListAsync(cancellationToken);

        foreach (var blog in dueBlogs)
        {
            blog.Status = "Published";
            blog.UpdatedAt = utcNow;
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<BlogPost> BuildQuery(
        string? searchTerm,
        string? status,
        int? createdByAccountId,
        bool onlyPublished)
    {
        var query = _context.BlogPosts
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.ApprovedByNavigation)
            .Where(x => !x.IsDeleted);

        if (createdByAccountId.HasValue)
        {
            query = query.Where(x => x.AccountId == createdByAccountId.Value);
        }

        if (onlyPublished)
        {
            query = query.Where(x => x.Status == "Published");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status.ToLower() == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim();
            query = query.Where(x =>
                x.BlogTitle.Contains(normalizedSearchTerm) ||
                x.BlogContent.Contains(normalizedSearchTerm));
        }

        return query;
    }

    private static IQueryable<BlogPost> ApplySorting(IQueryable<BlogPost> query, string? sortBy, bool sortDesc)
    {
        return (sortBy?.Trim().ToLowerInvariant(), sortDesc) switch
        {
            ("blogtitle", true) => query.OrderByDescending(x => x.BlogTitle),
            ("blogtitle", false) => query.OrderBy(x => x.BlogTitle),
            ("status", true) => query.OrderByDescending(x => x.Status),
            ("status", false) => query.OrderBy(x => x.Status),
            ("blogat", true) => query.OrderByDescending(x => x.BlogAt),
            ("blogat", false) => query.OrderBy(x => x.BlogAt),
            ("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
            ("createdat", false) => query.OrderBy(x => x.CreatedAt),
            ("updatedat", true) => query.OrderByDescending(x => x.UpdatedAt),
            ("updatedat", false) => query.OrderBy(x => x.UpdatedAt),
            (_, true) => query.OrderByDescending(x => x.BlogPostId),
            _ => query.OrderBy(x => x.BlogPostId)
        };
    }
}
