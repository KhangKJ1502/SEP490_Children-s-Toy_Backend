using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Repository triển khai các thao tác truy vấn và biến đổi dữ liệu cho Đánh giá sản phẩm (ReviewProduct),
/// hình ảnh, phản hồi nhân viên, nhật ký kiểm duyệt và tương tác Like trong cơ sở dữ liệu Entity Framework Core.
/// </summary>
public class ReviewRepository : IReviewRepository
{
    private readonly SEP490ToyStoreContext _context;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Khởi tạo ReviewRepository với DbContext và TimeProvider.
    /// </summary>
    /// <param name="context">DbContext kết nối cơ sở dữ liệu.</param>
    /// <param name="timeProvider">Provider cung cấp và chuyển đổi thời gian UTC/Local.</param>
    public ReviewRepository(SEP490ToyStoreContext context, ITimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    // ==========================================
    // Public / Customer Queries
    // ==========================================

    /// <summary>
    /// Lấy danh sách đánh giá công khai đã được duyệt (APPROVED) của sản phẩm kèm phân trang, lọc và sắp xếp.
    /// Chỉ nạp các hình ảnh đã được duyệt và phản hồi chưa xóa mềm.
    /// </summary>
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

        // Áp dụng sắp xếp động: theo Rating hoặc CreatedAt (mặc định)
        query = sortBy?.ToLower() switch
        {
            "rating" => sortDesc ? query.OrderByDescending(r => r.Rating) : query.OrderBy(r => r.Rating),
            _ => sortDesc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt)
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

    /// <summary>
    /// Đếm tổng số đánh giá công khai thỏa mãn điều kiện lọc của sản phẩm.
    /// </summary>
    public async Task<int> GetPublicCountAsync(
        int productId,
        byte? rating,
        bool? hasImage,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        return await BuildPublicQuery(productId, rating, hasImage, searchTerm).CountAsync(cancellationToken);
    }

    /// <summary>
    /// Xây dựng IQueryable cho danh sách đánh giá công khai: chỉ lấy bản ghi chưa xóa mềm, đúng ProductId và ModerationStatus = 'Approved'.
    /// </summary>
    private IQueryable<ReviewProduct> BuildPublicQuery(
        int productId,
        byte? rating,
        bool? hasImage,
        string? searchTerm)
    {
        var query = _context.ReviewProducts
            .Where(r => !r.IsDeleted && r.ProductId == productId && r.ModerationStatus == "Approved");

        // Lọc theo số sao đánh giá (1-5 sao)
        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        // Lọc theo đánh giá có/không có hình ảnh kèm theo
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

        // Lọc theo từ khóa trong nội dung bình luận
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(r => r.Comment != null && r.Comment.Contains(searchTerm));
        }

        return query;
    }

    /// <summary>
    /// Lấy chi tiết một đánh giá công khai (chỉ khi ModerationStatus = 'Approved' và IsDeleted = false).
    /// </summary>
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

    /// <summary>
    /// Lấy đánh giá kèm danh sách hình ảnh và sản phẩm để phục vụ cập nhật.
    /// </summary>
    public async Task<ReviewProduct?> GetByIdForUpdateAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewProducts
            .Include(r => r.Product)
            .Include(r => r.Order)
            .Include(r => r.ReviewProductImages.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && !r.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Kiểm tra xem khách hàng đã từng đánh giá sản phẩm này trong đơn hàng này hay chưa (mỗi sản phẩm trong 1 đơn chỉ đánh giá 1 lần).
    /// </summary>
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

    /// <summary>
    /// Lấy đơn hàng và chi tiết đơn hàng của khách hàng để kiểm tra tính hợp lệ trước khi tạo review.
    /// </summary>
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

    /// <summary>
    /// Thêm thực thể ReviewProduct vào DbContext.
    /// </summary>
    public async Task AddReviewAsync(ReviewProduct review, CancellationToken cancellationToken = default)
    {
        await _context.ReviewProducts.AddAsync(review, cancellationToken);
    }

    /// <summary>
    /// Thêm thực thể ReviewProductImage vào DbContext.
    /// </summary>
    public async Task AddImageAsync(ReviewProductImage image, CancellationToken cancellationToken = default)
    {
        await _context.ReviewProductImages.AddAsync(image, cancellationToken);
    }

    /// <summary>
    /// Thêm thực thể ReviewModerationLog vào DbContext.
    /// </summary>
    public async Task AddModerationLogAsync(ReviewModerationLog log, CancellationToken cancellationToken = default)
    {
        await _context.ReviewModerationLogs.AddAsync(log, cancellationToken);
    }

    /// <summary>
    /// Đánh dấu cập nhật thực thể ReviewProduct.
    /// </summary>
    public void Update(ReviewProduct review)
    {
        _context.Update(review);
    }

    // ==========================================
    // Customer My Reviews Queries
    // ==========================================

    /// <summary>
    /// Lấy danh sách sản phẩm chưa được đánh giá từ các đơn hàng hoàn tất (trong vòng 20 ngày gần nhất) của khách hàng.
    /// </summary>
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

    /// <summary>
    /// Đếm tổng số sản phẩm chưa được đánh giá của khách hàng.
    /// </summary>
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

    /// <summary>
    /// Lấy danh sách tất cả các đánh giá mà khách hàng hiện tại đã viết, kèm phân trang và lọc theo trạng thái kiểm duyệt.
    /// </summary>
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

        // Áp dụng sắp xếp
        query = sortBy?.ToLower() switch
        {
            "rating" => sortDesc ? query.OrderByDescending(r => r.Rating) : query.OrderBy(r => r.Rating),
            _ => sortDesc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt)
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

    /// <summary>
    /// Đếm tổng số đánh giá của khách hàng.
    /// </summary>
    public async Task<int> GetMyReviewsCountAsync(
        int accountId,
        string? moderationStatus,
        CancellationToken cancellationToken = default)
    {
        return await BuildMyReviewsQuery(accountId, moderationStatus).CountAsync(cancellationToken);
    }

    /// <summary>
    /// Xây dựng IQueryable cho danh sách đánh giá của khách hàng cá nhân.
    /// </summary>
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

    // ==========================================
    // Admin / Staff Queries & Actions
    // ==========================================

    /// <summary>
    /// Lấy danh sách đánh giá toàn hệ thống phục vụ Admin/Staff quản trị với đầy đủ các tiêu chí lọc nâng cao.
    /// </summary>
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

        // Áp dụng sắp xếp
        query = sortBy?.ToLower() switch
        {
            "rating" => sortDesc ? query.OrderByDescending(r => r.Rating) : query.OrderBy(r => r.Rating),
            _ => sortDesc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt)
        };

        return await query
            .Include(r => r.Account)
            .Include(r => r.Product)
            .Include(r => r.Order)
            .Include(r => r.ReviewProductImages)
            .Include(r => r.StaffReviewProductReplies)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Đếm tổng số đánh giá thỏa mãn điều kiện lọc của Admin/Staff.
    /// </summary>
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

    /// <summary>
    /// Xây dựng IQueryable cho bộ lọc quản trị Admin/Staff (lọc theo cờ xóa mềm, trạng thái duyệt, sản phẩm, tài khoản, đơn hàng, ngày tạo, từ khóa).
    /// </summary>
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

        // Lọc theo cờ xóa mềm (mặc định chỉ lấy chưa xóa)
        if (isDeleted.HasValue)
        {
            query = query.Where(r => r.IsDeleted == isDeleted.Value);
        }
        else
        {
            query = query.Where(r => !r.IsDeleted);
        }

        // Lọc theo trạng thái kiểm duyệt (PENDING_APPROVAL, APPROVED, REJECTED, HIDDEN)
        if (!string.IsNullOrWhiteSpace(moderationStatus))
        {
            query = query.Where(r => r.ModerationStatus == moderationStatus);
        }

        // Lọc theo sản phẩm
        if (productId.HasValue)
        {
            query = query.Where(r => r.ProductId == productId.Value);
        }

        // Lọc theo tài khoản khách hàng
        if (accountId.HasValue)
        {
            query = query.Where(r => r.AccountId == accountId.Value);
        }

        // Lọc theo đơn hàng
        if (orderId.HasValue)
        {
            query = query.Where(r => r.OrderId == orderId.Value);
        }

        // Lọc theo khoảng ngày tạo
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

        // Tìm kiếm theo từ khóa trong comment, tên sản phẩm hoặc tên tài khoản
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(r => 
                (r.Comment != null && r.Comment.Contains(searchTerm)) ||
                r.Product.ProductName.Contains(searchTerm) ||
                r.Account.AccountName.Contains(searchTerm));
        }

        return query;
    }

    /// <summary>
    /// Lấy chi tiết toàn diện của một đánh giá cho Admin/Staff (kèm hình ảnh, đơn hàng, khách hàng, sản phẩm, log kiểm duyệt, phản hồi staff).
    /// </summary>
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

    /// <summary>
    /// Lấy thông tin phản hồi của nhân viên theo ID phản hồi.
    /// </summary>
    public async Task<StaffReviewProductReply?> GetReplyByIdAsync(int replyId, CancellationToken cancellationToken = default)
    {
        return await _context.StaffReviewProductReplies
            .FirstOrDefaultAsync(r => r.ReplyProductId == replyId && !r.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Thêm mới bản ghi phản hồi của nhân viên vào DbContext.
    /// </summary>
    public async Task AddReplyAsync(StaffReviewProductReply reply, CancellationToken cancellationToken = default)
    {
        await _context.StaffReviewProductReplies.AddAsync(reply, cancellationToken);
    }

    /// <summary>
    /// Lấy thông tin cảm xúc (Like) của khách hàng trên một đánh giá cụ thể.
    /// </summary>
    public async Task<ReviewProductReaction?> GetReactionAsync(int reviewId, int accountId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewProductReactions
            .FirstOrDefaultAsync(r => r.ReviewProductId == reviewId && r.AccountId == accountId, cancellationToken);
    }

    /// <summary>
    /// Thêm mới bản ghi cảm xúc (Reaction) của khách hàng vào DbContext.
    /// </summary>
    public async Task AddReactionAsync(ReviewProductReaction reaction, CancellationToken cancellationToken = default)
    {
        await _context.ReviewProductReactions.AddAsync(reaction, cancellationToken);
    }

    /// <summary>
    /// Đếm tổng số lượng Like hợp lệ của một đánh giá.
    /// </summary>
    public async Task<int> GetLikeCountAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewProductReactions
            .CountAsync(r => r.ReviewProductId == reviewId && !r.IsDeleted && r.ReactionTypeNavigation.Code.ToLower() == "like", cancellationToken);
    }

    /// <summary>
    /// Lấy loại cảm xúc theo mã code (ví dụ: "LIKE").
    /// </summary>
    public async Task<ReactionType?> GetReactionTypeByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();
        return await _context.ReactionTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code.ToLower() == normalizedCode, cancellationToken);
    }
}
