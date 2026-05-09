using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly SEP490ToyStoreContext _context;

    public ReviewRepository(SEP490ToyStoreContext context)
    {
        _context = context;
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
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && !r.IsDeleted && r.ModerationStatus == "Approved", cancellationToken);
    }

    public async Task<ReviewProduct?> GetByIdForUpdateAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewProducts
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
            query = query.Where(r => r.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= toDate.Value);
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
}
