using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class BlogRepository : IBlogRepository
{
    private readonly SEP490ToyStoreContext _context;
    private static readonly string[] AdminVisibleStatuses = ["Pending", "Published", "Scheduled", "Hidden"];

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
        bool featuredOnly = false,
        int? createdByAccountId = null,
        bool onlyPublished = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(searchTerm, status, featuredOnly, createdByAccountId, onlyPublished);
        query = ApplySorting(query, sortBy, sortDesc);

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        string? searchTerm = null,
        string? status = null,
        bool featuredOnly = false,
        int? createdByAccountId = null,
        bool onlyPublished = false,
        CancellationToken cancellationToken = default)
    {
        return BuildQuery(searchTerm, status, featuredOnly, createdByAccountId, onlyPublished).CountAsync(cancellationToken);
    }

    public Task<BlogPost?> GetByIdAsync(int blogPostId, CancellationToken cancellationToken = default)
    {
        return _context.BlogPosts
            .Where(x => !x.IsDeleted)
            .Include(x => x.Account)
            .Include(x => x.ApprovedByNavigation)
            .Include(x => x.BlogCategory)
            .Include(x => x.BlogPostStat)
            .FirstOrDefaultAsync(x => x.BlogPostId == blogPostId, cancellationToken);
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

    public async Task<BlogPost> HideAsync(BlogPost entity, CancellationToken cancellationToken = default)
    {
        _context.BlogPosts.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private IQueryable<BlogPost> BuildQuery(
        string? searchTerm,
        string? status,
        bool featuredOnly,
        int? createdByAccountId,
        bool onlyPublished)
    {
        IQueryable<BlogPost> query = _context.BlogPosts
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Include(x => x.Account)
            .Include(x => x.ApprovedByNavigation)
            .Include(x => x.BlogCategory)
            .Include(x => x.BlogPostStat);

        if (createdByAccountId.HasValue)
        {
            query = query.Where(x => x.AccountId == createdByAccountId.Value);
        }

        if (onlyPublished)
        {
            query = query.Where(x => x.Status == "Published");
        }

        // Admin listing (no owner filter, not public search) only shows workflow states
        // that are relevant for review and published visibility.
        if (!createdByAccountId.HasValue && !onlyPublished)
        {
            query = query.Where(x => AdminVisibleStatuses.Contains(x.Status));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();
            if (normalizedStatus == "hidden")
            {
                query = query.Where(x => x.Status == "Hidden");
            }
            else
            {
                query = query.Where(x => x.Status.ToLower() == normalizedStatus);
            }
        }

        if (featuredOnly)
        {
            query = query.Where(x => x.IsFeatured);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim();
            query = query.Where(x => x.BlogTitle.Contains(normalizedSearchTerm));
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
            ("featured", true) => query
                .OrderByDescending(x => x.IsFeatured)
                .ThenByDescending(x => x.CreatedAt),
            ("featured", false) => query
                .OrderBy(x => x.IsFeatured)
                .ThenBy(x => x.CreatedAt),
            ("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
            ("createdat", false) => query.OrderBy(x => x.CreatedAt),
            ("interaction", true) => query
                .OrderByDescending(x => (x.BlogPostStat != null ? x.BlogPostStat.LikeCount : 0) + (x.BlogPostStat != null ? x.BlogPostStat.CommentCount : 0))
                .ThenByDescending(x => x.CreatedAt),
            ("interaction", false) => query
                .OrderBy(x => (x.BlogPostStat != null ? x.BlogPostStat.LikeCount : 0) + (x.BlogPostStat != null ? x.BlogPostStat.CommentCount : 0))
                .ThenBy(x => x.CreatedAt),
            ("updatedat", true) => query.OrderByDescending(x => x.UpdatedAt),
            ("updatedat", false) => query.OrderBy(x => x.UpdatedAt),
            (_, true) => query.OrderByDescending(x => x.BlogPostId),
            _ => query.OrderBy(x => x.BlogPostId)
        };
    }
}
