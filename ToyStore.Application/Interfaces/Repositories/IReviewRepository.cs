using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IReviewRepository
{
    // --- Public / Customer ---
    Task<List<ReviewProduct>> GetPublicPagedAsync(
        int productId,
        int pageNumber,
        int pageSize,
        string? sortBy,
        bool sortDesc,
        byte? rating,
        bool? hasImage,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<int> GetPublicCountAsync(
        int productId,
        byte? rating,
        bool? hasImage,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<ReviewProduct?> GetByIdPublicAsync(int reviewId, CancellationToken cancellationToken = default);

    Task<ReviewProduct?> GetByIdForUpdateAsync(int reviewId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByAccountOrderProductAsync(
        int accountId, 
        int orderId, 
        int productId, 
        CancellationToken cancellationToken = default);

    Task<Order?> GetOrderForReviewAsync(
        int orderId, 
        int accountId, 
        CancellationToken cancellationToken = default);

    Task AddReviewAsync(ReviewProduct review, CancellationToken cancellationToken = default);
    Task AddImageAsync(ReviewProductImage image, CancellationToken cancellationToken = default);
    Task AddModerationLogAsync(ReviewModerationLog log, CancellationToken cancellationToken = default);

    // --- Customer My Reviews ---
    Task<List<OrderDetail>> GetUnreviewedProductsAsync(int accountId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetUnreviewedProductsCountAsync(int accountId, CancellationToken cancellationToken = default);
    Task<List<ReviewProduct>> GetMyReviewsPagedAsync(
        int accountId,
        int pageNumber,
        int pageSize,
        string? sortBy,
        bool sortDesc,
        string? moderationStatus,
        CancellationToken cancellationToken = default);
    Task<int> GetMyReviewsCountAsync(
        int accountId,
        string? moderationStatus,
        CancellationToken cancellationToken = default);

    // --- Admin / Staff ---
    Task<List<ReviewProduct>> GetAdminPagedAsync(
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
        CancellationToken cancellationToken = default);

    Task<int> GetAdminCountAsync(
        string? moderationStatus,
        int? productId,
        int? accountId,
        int? orderId,
        string? searchTerm,
        DateTime? fromDate,
        DateTime? toDate,
        bool? isDeleted,
        CancellationToken cancellationToken = default);

    Task<ReviewProduct?> GetByIdForAdminAsync(int reviewId, CancellationToken cancellationToken = default);

    Task<StaffReviewProductReply?> GetReplyByIdAsync(int replyId, CancellationToken cancellationToken = default);
    Task AddReplyAsync(StaffReviewProductReply reply, CancellationToken cancellationToken = default);
    
    Task<ReviewProductReaction?> GetReactionAsync(int reviewId, int accountId, CancellationToken cancellationToken = default);
    Task AddReactionAsync(ReviewProductReaction reaction, CancellationToken cancellationToken = default);
    Task<int> GetLikeCountAsync(int reviewId, CancellationToken cancellationToken = default);
    Task<ReactionType?> GetReactionTypeByCodeAsync(string code, CancellationToken cancellationToken = default);
}
