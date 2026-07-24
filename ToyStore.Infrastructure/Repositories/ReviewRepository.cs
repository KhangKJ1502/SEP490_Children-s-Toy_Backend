using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly SEP490ToyStoreContext _context;
    private readonly ITimeProvider _timeProvider;

    public ReviewRepository(SEP490ToyStoreContext context, ITimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    // --- Public / Customer ---

    public async Task<List<ReviewProduct>> GetPublicPagedAsync(
        int productId,
        int pageNumber,
        int pageSize,
        string? sortBy,
        bool sortDesc,
        byte? rating,
        bool? hasImage,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = BuildPublicQuery(productId, rating, hasImage, searchTerm);

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "rating" => sortDesc ? query.OrderByDescending(r => r.Rating) : query.OrderBy(r => r.Rating),
            _ => sortDesc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt) // Default sort by CreatedAt
        };

        return await query
            .Include(r => r.Account)
            .Include(r => r.ReviewProductImages.Where(i => !i.IsDeleted && i.ModerationStatus == "Approved"))
            .Include(r => r.StaffReviewProductReplies.Where(reply => !reply.IsDeleted))
                .ThenInclude(reply => reply.Staff)
            .Include(r => r.ReviewProductReactions.Where(reaction => !reaction.IsDeleted))
                .ThenInclude(reaction => reaction.ReactionTypeNavigation)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetPublicCountAsync(
        int productId,
        byte? rating,
        bool? hasImage,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        return await BuildPublicQuery(productId, rating, hasImage, searchTerm).CountAsync(cancellationToken);
    }

    private IQueryable<ReviewProduct> BuildPublicQuery(
        int productId,
        byte? rating,
        bool? hasImage,
        string? searchTerm)
    {
        var query = _context.ReviewProducts
            .Where(r => !r.IsDeleted && r.ProductId == productId && r.ModerationStatus == "Approved");

        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        if (hasImage.HasValue)
        {
            if (hasImage.Value)
            {
                query = query.Where(r => r.ReviewProductImages.Any(i => !i.IsDeleted && i.ModerationStatus == "Approved"));
            }
            else
            {
                query = query.Where(r => !r.ReviewProductImages.Any(i => !i.IsDeleted && i.ModerationStatus == "Approved"));
            }
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(r => r.Comment != null && r.Comment.Contains(searchTerm));
        }

        return query;
    }

    public async Task<ReviewProduct?> GetByIdPublicAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewProducts
            .AsNoTracking()
            .Include(r => r.Account)
            .Include(r => r.ReviewProductImages.Where(i => !i.IsDeleted && i.ModerationStatus == "Approved"))
            .Include(r => r.StaffReviewProductReplies.Where(reply => !reply.IsDeleted))
                .ThenInclude(reply => reply.Staff)
            .Include(r => r.ReviewProductReactions.Where(reaction => !reaction.IsDeleted))
                .ThenInclude(reaction => reaction.ReactionTypeNavigation)
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && !r.IsDeleted && r.ModerationStatus == "Approved", cancellationToken);
    }

    public async Task<ReviewProduct?> GetByIdForUpdateAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewProducts
            .Include(r => r.Product)
            .Include(r => r.Order)
            .Include(r => r.ReviewProductImages.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && !r.IsDeleted, cancellationToken);
    }

    public async Task<bool> ExistsByAccountOrderProductAsync(
        int accountId, 
        int orderId, 
        int productId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.ReviewProducts
            .AnyAsync(r => !r.IsDeleted && 
                           r.AccountId == accountId && 
                           r.OrderId == orderId && 
                           r.ProductId == productId, cancellationToken);
    }

    public async Task<Order?> GetOrderForReviewAsync(
        int orderId, 
        int accountId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails)
            .Include(o => o.Status)
            .FirstOrDefaultAsync(o => !o.IsDeleted && o.OrderId == orderId && o.AccountId == accountId, cancellationToken);
    }

    public async Task AddReviewAsync(ReviewProduct review, CancellationToken cancellationToken = default)
    {
        await _context.ReviewProducts.AddAsync(review, cancellationToken);
    }

    public async Task AddImageAsync(ReviewProductImage image, CancellationToken cancellationToken = default)
    {
        await _context.ReviewProductImages.AddAsync(image, cancellationToken);
    }

    public async Task AddModerationLogAsync(ReviewModerationLog log, CancellationToken cancellationToken = default)
    {
        await _context.ReviewModerationLogs.AddAsync(log, cancellationToken);
    }

    public void Update(ReviewProduct review)
    {
        _context.Update(review);
    }

    // --- Customer My Reviews ---

    public async Task<List<OrderDetail>> GetUnreviewedProductsAsync(int accountId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-20);
        return await _context.OrderDetails
            .AsNoTracking()
            .Include(od => od.Order)
                .ThenInclude(o => o.Status)
            .Where(od => !od.Order.IsDeleted &&
                         od.Order.AccountId == accountId &&
                         od.Order.Status.StatusName == "Completed" &&
                         od.Order.CompletedAt >= cutoffDate &&
                         !_context.ReviewProducts.Any(r => !r.IsDeleted && 
                                                           r.AccountId == accountId && 
                                                           r.OrderId == od.OrderId && 
                                                           r.ProductId == od.ProductId))
            .OrderByDescending(od => od.Order.CompletedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreviewedProductsCountAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-20);
        return await _context.OrderDetails
            .AsNoTracking()
            .Where(od => !od.Order.IsDeleted &&
                         od.Order.AccountId == accountId &&
                         od.Order.Status.StatusName == "Completed" &&
                         od.Order.CompletedAt >= cutoffDate &&
                         !_context.ReviewProducts.Any(r => !r.IsDeleted && 
                                                           r.AccountId == accountId && 
                                                           r.OrderId == od.OrderId && 
                                                           r.ProductId == od.ProductId))
            .CountAsync(cancellationToken);
    }

    public async Task<List<ReviewProduct>> GetMyReviewsPagedAsync(
        int accountId,
        int pageNumber,
        int pageSize,
        string? sortBy,
        bool sortDesc,
        string? moderationStatus,
        CancellationToken cancellationToken = default)
    {
        var query = BuildMyReviewsQuery(accountId, moderationStatus);

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "rating" => sortDesc ? query.OrderByDescending(r => r.Rating) : query.OrderBy(r => r.Rating),
            _ => sortDesc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt) // Default
        };

        return await query
            .Include(r => r.Product)
                .ThenInclude(p => p.ProductImage)
            .Include(r => r.Order)
            .Include(r => r.ReviewProductImages.Where(i => !i.IsDeleted))
            .Include(r => r.StaffReviewProductReplies.Where(reply => !reply.IsDeleted))
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetMyReviewsCountAsync(
        int accountId,
        string? moderationStatus,
        CancellationToken cancellationToken = default)
    {
        return await BuildMyReviewsQuery(accountId, moderationStatus).CountAsync(cancellationToken);
    }

    private IQueryable<ReviewProduct> BuildMyReviewsQuery(int accountId, string? moderationStatus)
    {
        var query = _context.ReviewProducts
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.AccountId == accountId);

        if (!string.IsNullOrWhiteSpace(moderationStatus))
        {
            query = query.Where(r => r.ModerationStatus == moderationStatus);
        }

        return query;
    }

    // --- Admin / Staff ---

    public async Task<List<ReviewProduct>> GetAdminPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy,
        bool sortDesc,
        string? moderationStatus,
        int? productId,
        int? accountId,
        int? orderId,
        string? searchTerm,
        DateTime? fromDate,
        DateTime? toDate,
        bool? isDeleted,
        CancellationToken cancellationToken = default)
    {
        var query = BuildAdminQuery(moderationStatus, productId, accountId, orderId, searchTerm, fromDate, toDate, isDeleted);

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "rating" => sortDesc ? query.OrderByDescending(r => r.Rating) : query.OrderBy(r => r.Rating),
            _ => sortDesc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt) // Default sort by CreatedAt
        };

        return await query
            .Include(r => r.Account)
            .Include(r => r.Product)
            .Include(r => r.Order)
            .Include(r => r.ReviewProductImages) // Don't filter IsDeleted for admin list count, or let automapper handle
            .Include(r => r.StaffReviewProductReplies) // Don't filter IsDeleted for admin list count
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetAdminCountAsync(
        string? moderationStatus,
        int? productId,
        int? accountId,
        int? orderId,
        string? searchTerm,
        DateTime? fromDate,
        DateTime? toDate,
        bool? isDeleted,
        CancellationToken cancellationToken = default)
    {
        return await BuildAdminQuery(moderationStatus, productId, accountId, orderId, searchTerm, fromDate, toDate, isDeleted).CountAsync(cancellationToken);
    }

    private IQueryable<ReviewProduct> BuildAdminQuery(
        string? moderationStatus,
        int? productId,
        int? accountId,
        int? orderId,
        string? searchTerm,
        DateTime? fromDate,
        DateTime? toDate,
        bool? isDeleted)
    {
        var query = _context.ReviewProducts.AsQueryable();

        if (isDeleted.HasValue)
        {
            query = query.Where(r => r.IsDeleted == isDeleted.Value);
        }
        else
        {
            query = query.Where(r => !r.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(moderationStatus))
        {
            query = query.Where(r => r.ModerationStatus == moderationStatus);
        }

        if (productId.HasValue)
        {
            query = query.Where(r => r.ProductId == productId.Value);
        }

        if (accountId.HasValue)
        {
            query = query.Where(r => r.AccountId == accountId.Value);
        }

        if (orderId.HasValue)
        {
            query = query.Where(r => r.OrderId == orderId.Value);
        }

        if (fromDate.HasValue)
        {
            var startUtc = _timeProvider.ToUtc(fromDate.Value.Date);
            query = query.Where(r => r.CreatedAt >= startUtc);
        }

        if (toDate.HasValue)
        {
            var endUtc = _timeProvider.ToUtc(toDate.Value.Date.AddDays(1));
            query = query.Where(r => r.CreatedAt < endUtc);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(r => 
                (r.Comment != null && r.Comment.Contains(searchTerm)) ||
                r.Product.ProductName.Contains(searchTerm) ||
                r.Account.AccountName.Contains(searchTerm));
        }

        return query;
    }

    public async Task<ReviewProduct?> GetByIdForAdminAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewProducts
            .AsNoTracking()
            .Include(r => r.Account)
            .Include(r => r.Product)
            .Include(r => r.Order)
            .Include(r => r.ReviewProductImages.Where(i => !i.IsDeleted))
            .Include(r => r.StaffReviewProductReplies.Where(reply => !reply.IsDeleted))
                .ThenInclude(reply => reply.Staff)
            .Include(r => r.ReviewModerationLogs)
                .ThenInclude(log => log.ModeratedByNavigation)
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId, cancellationToken);
    }

    public async Task<StaffReviewProductReply?> GetReplyByIdAsync(int replyId, CancellationToken cancellationToken = default)
    {
        return await _context.StaffReviewProductReplies
            .FirstOrDefaultAsync(r => r.ReplyProductId == replyId && !r.IsDeleted, cancellationToken);
    }

    public async Task AddReplyAsync(StaffReviewProductReply reply, CancellationToken cancellationToken = default)
    {
        await _context.StaffReviewProductReplies.AddAsync(reply, cancellationToken);
    }

    public async Task<ReviewProductReaction?> GetReactionAsync(int reviewId, int accountId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewProductReactions
            .FirstOrDefaultAsync(r => r.ReviewProductId == reviewId && r.AccountId == accountId, cancellationToken);
    }

    public async Task AddReactionAsync(ReviewProductReaction reaction, CancellationToken cancellationToken = default)
    {
        await _context.ReviewProductReactions.AddAsync(reaction, cancellationToken);
    }

    public async Task<int> GetLikeCountAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewProductReactions
            .CountAsync(r => r.ReviewProductId == reviewId && !r.IsDeleted && r.ReactionTypeNavigation.Code.ToLower() == "like", cancellationToken);
    }

    public async Task<ReactionType?> GetReactionTypeByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();
        return await _context.ReactionTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code.ToLower() == normalizedCode, cancellationToken);
    }
}
