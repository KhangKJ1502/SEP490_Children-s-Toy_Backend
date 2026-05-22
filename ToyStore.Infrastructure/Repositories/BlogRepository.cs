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

    public Task<List<BlogCategory>> GetBlogCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return _context.BlogCategories
            .AsNoTracking()
            .OrderBy(x => x.BlogCategoriesName)
            .ToListAsync(cancellationToken);
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
                && (x.Status == "Scheduled" || x.Status == "Approved")
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

    public Task<List<ReviewBlog>> GetReviewsByBlogIdAsync(int blogPostId, bool includeHidden, CancellationToken cancellationToken = default)
    {
        var query = _context.ReviewBlogs
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.BlogPost)
            .Where(x => x.BlogPostId == blogPostId);

        if (!includeHidden)
        {
            query = query.Where(x => !x.IsDeleted);
        }

        return query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ReviewBlogReply>> GetRepliesByReviewIdsAsync(List<int> reviewIds, bool includeHidden, CancellationToken cancellationToken = default)
    {
        var query = _context.ReviewBlogReplies
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.ReplyToAccount)
            .Where(x => reviewIds.Contains(x.ReviewBlogId));

        if (!includeHidden)
        {
            query = query.Where(x => !x.IsDeleted);
        }

        return query
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<ReviewBlog?> GetReviewByIdAsync(int reviewBlogId, CancellationToken cancellationToken = default)
    {
        return _context.ReviewBlogs
            .Include(x => x.Account)
            .Include(x => x.BlogPost)
            .FirstOrDefaultAsync(x => x.ReviewBlogId == reviewBlogId, cancellationToken);
    }

    public Task<ReviewBlogReply?> GetReplyByIdAsync(int replyBlogId, CancellationToken cancellationToken = default)
    {
        return _context.ReviewBlogReplies
            .Include(x => x.Account)
            .Include(x => x.ReplyToAccount)
            .Include(x => x.ReviewBlog)
            .FirstOrDefaultAsync(x => x.ReplyBlogId == replyBlogId, cancellationToken);
    }

    public async Task<ReviewBlog> CreateReviewAsync(ReviewBlog entity, CancellationToken cancellationToken = default)
    {
        await _context.ReviewBlogs.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<ReviewBlogReply> CreateReplyAsync(ReviewBlogReply entity, CancellationToken cancellationToken = default)
    {
        await _context.ReviewBlogReplies.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateReviewAsync(ReviewBlog entity, CancellationToken cancellationToken = default)
    {
        _context.ReviewBlogs.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateReplyAsync(ReviewBlogReply entity, CancellationToken cancellationToken = default)
    {
        _context.ReviewBlogReplies.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<List<ReviewBlog>> GetPagedReviewsForManagementAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = BuildReviewManagementQuery(searchTerm, status)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        return query.ToListAsync(cancellationToken);
    }

    public Task<int> CountReviewsForManagementAsync(string? searchTerm, string? status, CancellationToken cancellationToken = default)
    {
        return BuildReviewManagementQuery(searchTerm, status).CountAsync(cancellationToken);
    }

    public Task<List<BlogCommentBanReason>> GetBlogCommentBanReasonsAsync(CancellationToken cancellationToken = default)
    {
        return _context.BlogCommentBanReasons
            .AsNoTracking()
            .OrderBy(x => x.BanReasonId)
            .ToListAsync(cancellationToken);
    }

    public Task<BlogCommentModerationLog?> GetLatestRejectedCommentLogAsync(int reviewBlogId, CancellationToken cancellationToken = default)
    {
        return _context.BlogCommentModerationLogs
            .AsNoTracking()
            .Include(x => x.BanReason)
            .Where(x => x.TargetType == "Comment" && x.CommentId == reviewBlogId && x.Action == "Rejected")
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<BlogCommentModerationLog?> GetLatestRejectedReplyLogAsync(int replyBlogId, CancellationToken cancellationToken = default)
    {
        return _context.BlogCommentModerationLogs
            .AsNoTracking()
            .Include(x => x.BanReason)
            .Where(x => x.TargetType == "Reply" && x.ReplyId == replyBlogId && x.Action == "Rejected")
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddCommentModerationLogAsync(BlogCommentModerationLog log, CancellationToken cancellationToken = default)
    {
        await _context.BlogCommentModerationLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<ReactionType?> GetReactionTypeByCodeAsync(string reactionCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = reactionCode.Trim().ToLowerInvariant();
        return _context.ReactionTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code.ToLower() == normalizedCode, cancellationToken);
    }

    public Task<BlogPostReaction?> GetBlogReactionAsync(int blogPostId, int accountId, CancellationToken cancellationToken = default)
    {
        return _context.BlogPostReactions
            .Include(x => x.ReactionType)
            .FirstOrDefaultAsync(x => x.BlogPostId == blogPostId && x.AccountId == accountId, cancellationToken);
    }

    public Task<ReviewBlogReaction?> GetReviewReactionAsync(int reviewBlogId, int accountId, CancellationToken cancellationToken = default)
    {
        return _context.ReviewBlogReactions
            .Include(x => x.ReactionType)
            .FirstOrDefaultAsync(x => x.ReviewBlogId == reviewBlogId && x.AccountId == accountId, cancellationToken);
    }

    public Task<ReviewBlogReplyReaction?> GetReplyReactionAsync(int replyBlogId, int accountId, CancellationToken cancellationToken = default)
    {
        return _context.ReviewBlogReplyReactions
            .Include(x => x.ReactionType)
            .FirstOrDefaultAsync(x => x.ReplyBlogId == replyBlogId && x.AccountId == accountId, cancellationToken);
    }

    public async Task UpsertBlogReactionAsync(int blogPostId, int accountId, int reactionTypeId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.BlogPostReactions
            .FirstOrDefaultAsync(x => x.BlogPostId == blogPostId && x.AccountId == accountId, cancellationToken);

        if (existing == null)
        {
            await _context.BlogPostReactions.AddAsync(new BlogPostReaction
            {
                BlogPostId = blogPostId,
                AccountId = accountId,
                ReactionTypeId = reactionTypeId,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }
        else
        {
            existing.ReactionTypeId = reactionTypeId;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertReviewReactionAsync(int reviewBlogId, int accountId, int reactionTypeId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ReviewBlogReactions
            .FirstOrDefaultAsync(x => x.ReviewBlogId == reviewBlogId && x.AccountId == accountId, cancellationToken);

        if (existing == null)
        {
            await _context.ReviewBlogReactions.AddAsync(new ReviewBlogReaction
            {
                ReviewBlogId = reviewBlogId,
                AccountId = accountId,
                ReactionTypeId = reactionTypeId,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }
        else
        {
            existing.ReactionTypeId = reactionTypeId;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertReplyReactionAsync(int replyBlogId, int accountId, int reactionTypeId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ReviewBlogReplyReactions
            .FirstOrDefaultAsync(x => x.ReplyBlogId == replyBlogId && x.AccountId == accountId, cancellationToken);

        if (existing == null)
        {
            await _context.ReviewBlogReplyReactions.AddAsync(new ReviewBlogReplyReaction
            {
                ReplyBlogId = replyBlogId,
                AccountId = accountId,
                ReactionTypeId = reactionTypeId,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }
        else
        {
            existing.ReactionTypeId = reactionTypeId;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveBlogReactionAsync(int blogPostId, int accountId, CancellationToken cancellationToken = default)
    {
        var deletedCount = await _context.BlogPostReactions
            .Where(x => x.BlogPostId == blogPostId && x.AccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);
        return deletedCount > 0;
    }

    public async Task<bool> RemoveReviewReactionAsync(int reviewBlogId, int accountId, CancellationToken cancellationToken = default)
    {
        var deletedCount = await _context.ReviewBlogReactions
            .Where(x => x.ReviewBlogId == reviewBlogId && x.AccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);
        return deletedCount > 0;
    }

    public async Task<bool> RemoveReplyReactionAsync(int replyBlogId, int accountId, CancellationToken cancellationToken = default)
    {
        var deletedCount = await _context.ReviewBlogReplyReactions
            .Where(x => x.ReplyBlogId == replyBlogId && x.AccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);
        return deletedCount > 0;
    }

    public async Task<Dictionary<string, int>> GetBlogReactionCountsAsync(int blogPostId, CancellationToken cancellationToken = default)
    {
        var counts = await _context.BlogPostReactions
            .AsNoTracking()
            .Where(x => x.BlogPostId == blogPostId)
            .GroupBy(x => x.ReactionType.Code)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Count);
    }

    public async Task<Dictionary<string, int>> GetReviewReactionCountsAsync(int reviewBlogId, CancellationToken cancellationToken = default)
    {
        var counts = await _context.ReviewBlogReactions
            .AsNoTracking()
            .Where(x => x.ReviewBlogId == reviewBlogId)
            .GroupBy(x => x.ReactionType.Code)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Count);
    }

    public async Task<Dictionary<string, int>> GetReplyReactionCountsAsync(int replyBlogId, CancellationToken cancellationToken = default)
    {
        var counts = await _context.ReviewBlogReplyReactions
            .AsNoTracking()
            .Where(x => x.ReplyBlogId == replyBlogId)
            .GroupBy(x => x.ReactionType.Code)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Count);
    }

    public async Task<Dictionary<int, Dictionary<string, int>>> GetReviewReactionCountsByIdsAsync(
        List<int> reviewBlogIds,
        CancellationToken cancellationToken = default)
    {
        if (reviewBlogIds.Count == 0)
        {
            return new Dictionary<int, Dictionary<string, int>>();
        }

        var raw = await _context.ReviewBlogReactions
            .AsNoTracking()
            .Where(x => reviewBlogIds.Contains(x.ReviewBlogId))
            .GroupBy(x => new { x.ReviewBlogId, Code = x.ReactionType.Code })
            .Select(x => new { x.Key.ReviewBlogId, x.Key.Code, Count = x.Count() })
            .ToListAsync(cancellationToken);

        return raw
            .GroupBy(x => x.ReviewBlogId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(
                    x => x.Code.ToLowerInvariant(),
                    x => x.Count));
    }

    public async Task<Dictionary<int, Dictionary<string, int>>> GetReplyReactionCountsByIdsAsync(
        List<int> replyBlogIds,
        CancellationToken cancellationToken = default)
    {
        if (replyBlogIds.Count == 0)
        {
            return new Dictionary<int, Dictionary<string, int>>();
        }

        var raw = await _context.ReviewBlogReplyReactions
            .AsNoTracking()
            .Where(x => replyBlogIds.Contains(x.ReplyBlogId))
            .GroupBy(x => new { x.ReplyBlogId, Code = x.ReactionType.Code })
            .Select(x => new { x.Key.ReplyBlogId, x.Key.Code, Count = x.Count() })
            .ToListAsync(cancellationToken);

        return raw
            .GroupBy(x => x.ReplyBlogId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(
                    x => x.Code.ToLowerInvariant(),
                    x => x.Count));
    }

    public async Task<Dictionary<int, string>> GetMyReviewReactionsByIdsAsync(
        List<int> reviewBlogIds,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        if (reviewBlogIds.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var raw = await _context.ReviewBlogReactions
            .AsNoTracking()
            .Where(x => x.AccountId == accountId && reviewBlogIds.Contains(x.ReviewBlogId))
            .Select(x => new { x.ReviewBlogId, x.ReactionType.Code })
            .ToListAsync(cancellationToken);

        return raw.ToDictionary(x => x.ReviewBlogId, x => x.Code);
    }

    public async Task<Dictionary<int, string>> GetMyReplyReactionsByIdsAsync(
        List<int> replyBlogIds,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        if (replyBlogIds.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var raw = await _context.ReviewBlogReplyReactions
            .AsNoTracking()
            .Where(x => x.AccountId == accountId && replyBlogIds.Contains(x.ReplyBlogId))
            .Select(x => new { x.ReplyBlogId, x.ReactionType.Code })
            .ToListAsync(cancellationToken);

        return raw.ToDictionary(x => x.ReplyBlogId, x => x.Code);
    }

    public async Task<(bool IsLocked, DateTime? LockedUntil)> CheckAndRefreshCommentLockAsync(
        int accountId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateViolationStateAsync(accountId, utcNow, cancellationToken);
        if (!state.IsCommentBanned)
        {
            return (false, null);
        }

        if (state.BanExpiresAt.HasValue && state.BanExpiresAt.Value <= utcNow)
        {
            state.IsCommentBanned = false;
            state.UnbannedAt = utcNow;
            state.UpdatedAt = utcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return (false, null);
        }

        return (true, state.BanExpiresAt);
    }

    public Task<List<BlogCommentViolationCount>> GetPagedBannedCommentAccountsAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        return BuildBannedCommentAccountsQuery(searchTerm)
            .OrderByDescending(x => x.BannedAt ?? x.UpdatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountBannedCommentAccountsAsync(string? searchTerm, CancellationToken cancellationToken = default)
    {
        return BuildBannedCommentAccountsQuery(searchTerm).CountAsync(cancellationToken);
    }

    public Task<BlogCommentViolationCount?> GetCommentPermissionStateAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        return _context.BlogCommentViolationCounts
            .Include(x => x.Account)
            .Include(x => x.UnbannedByNavigation)
            .FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
    }

    public async Task UpdateCommentPermissionStateAsync(
        BlogCommentViolationCount entity,
        CancellationToken cancellationToken = default)
    {
        _context.BlogCommentViolationCounts.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IncrementRateAndCheckCommentLimitAsync(
        int accountId,
        DateTime utcNow,
        int limit,
        int windowMinutes,
        CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateViolationStateAsync(accountId, utcNow, cancellationToken);
        var windowStart = utcNow.AddMinutes(-windowMinutes);

        if (!state.RateWindowAt.HasValue || state.RateWindowAt.Value <= windowStart)
        {
            state.RateWindowAt = utcNow;
            state.RateCount = 1;
            state.UpdatedAt = utcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return false;
        }

        var next = state.RateCount + 1;
        state.RateCount = next > byte.MaxValue ? byte.MaxValue : (byte)next;
        state.UpdatedAt = utcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return state.RateCount >= limit;
    }

    private async Task<BlogCommentViolationCount> GetOrCreateViolationStateAsync(
        int accountId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var state = await _context.BlogCommentViolationCounts
            .FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
        if (state != null)
        {
            return state;
        }

        state = new BlogCommentViolationCount
        {
            AccountId = accountId,
            ViolationCount = 0,
            LastViolatedAt = null,
            UpdatedAt = utcNow,
            IsCommentBanned = false,
            BannedAt = null,
            BanExpiresAt = null,
            UnbannedAt = null,
            UnbannedBy = null,
            RateCount = 0,
            RateWindowAt = null
        };

        await _context.BlogCommentViolationCounts.AddAsync(state, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return state;
    }

    private IQueryable<BlogCommentViolationCount> BuildBannedCommentAccountsQuery(string? searchTerm)
    {
        var query = _context.BlogCommentViolationCounts
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.UnbannedByNavigation)
            .Where(x => x.IsCommentBanned && !x.Account.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(x =>
                x.Account.AccountName.Contains(term)
                || x.Account.Email.Contains(term)
                || x.AccountId.ToString().Contains(term));
        }

        return query;
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

    private IQueryable<ReviewBlog> BuildReviewManagementQuery(string? searchTerm, string? status)
    {
        var query = _context.ReviewBlogs
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.BlogPost)
            .OrderByDescending(x => x.CreatedAt)
            .AsQueryable();

        query = query.Where(x =>
            !x.IsDeleted &&
            !x.IsHidden &&
            (x.ModerationStatus == "ManualReview" || x.ModerationStatus == "Approved"));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(x =>
                (x.Comment ?? string.Empty).Contains(term)
                || x.Account.AccountName.Contains(term)
                || x.BlogPost.BlogTitle.Contains(term)
                || x.ReviewBlogReplies.Any(r =>
                    (r.Comment ?? string.Empty).Contains(term)
                    || r.Account.AccountName.Contains(term)
                    || (r.ReplyToAccount != null && r.ReplyToAccount.AccountName.Contains(term))));
        }

        return query;
    }
}
