using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Defines blog business operations for staff/admin workflows and public search.
/// </summary>
public interface IBlogService
{
    /// <summary>
    /// Gets paginated blogs for admin view.
    /// </summary>
    Task<Result<PaginatedResponse<BlogListDto>>> GetBlogsForAdminAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        bool featuredOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated blogs created by current staff account.
    /// </summary>
    Task<Result<PaginatedResponse<BlogListDto>>> GetBlogsForStaffAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        bool featuredOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches published blogs for public usage.
    /// </summary>
    Task<Result<PaginatedResponse<BlogListDto>>> SearchPublishedBlogsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all blog categories.
    /// </summary>
    Task<Result<List<BlogCategoryDto>>> GetBlogCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets blog details by id with access control.
    /// </summary>
    Task<Result<BlogDetailDto>> GetBlogDetailsAsync(int blogPostId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new draft blog.
    /// </summary>
    Task<Result<BlogDetailDto>> CreateBlogAsync(CreateBlogDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing blog.
    /// </summary>
    Task<Result<BlogDetailDto>> UpdateBlogAsync(int blogPostId, UpdateBlogDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a draft blog to pending status.
    /// </summary>
    Task<Result<BlogDetailDto>> SubmitBlogAsync(int blogPostId, SubmitBlogDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves or rejects a pending blog.
    /// </summary>
    Task<Result<BlogDetailDto>> ApproveBlogAsync(int blogPostId, ApproveBlogDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a scheduled/approved blog immediately.
    /// </summary>
    Task<Result<BlogDetailDto>> PublishNowAsync(int blogPostId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hides a blog from public visibility.
    /// </summary>
    Task<Result<BlogDetailDto>> HideBlogAsync(int blogPostId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate nội dung bài blog bằng AI. Bao gồm validate, lấy source content (nếu cần), gọi AI gateway.
    /// </summary>
    Task<Result<AiBlogGenerateResult>> GenerateWithAiAsync(AiBlogGenerateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload thumbnail blog sau khi đã validate loại file và MIME type.
    /// Controller chỉ truyền stream + filename + contentType.
    /// </summary>
    Task<Result<UploadBlogThumbnailResponse>> UploadBlogThumbnailAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Result<List<BlogReviewDto>>> GetBlogReviewsAsync(int blogPostId, CancellationToken cancellationToken = default);

    Task<Result<BlogReviewDto>> CreateBlogReviewAsync(int blogPostId, CreateBlogReviewDto dto, CancellationToken cancellationToken = default);

    Task<Result<BlogReviewReplyDto>> CreateBlogReviewReplyAsync(int reviewBlogId, CreateBlogReviewReplyDto dto, CancellationToken cancellationToken = default);

    Task<Result<BlogReviewReplyDto>> CreateStaffBlogReviewReplyAsync(int reviewBlogId, CreateBlogReviewReplyDto dto, CancellationToken cancellationToken = default);

    Task<Result<bool>> RemoveBlogReviewAsync(int reviewBlogId, CancellationToken cancellationToken = default);

    Task<Result<PaginatedResponse<BlogReviewDto>>> GetBlogReviewsForManagementAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<Result<BlogReviewDto>> UpdateBlogReviewStatusAsync(int reviewBlogId, UpdateBlogReviewStatusDto dto, CancellationToken cancellationToken = default);

    Task<Result<BlogReviewReplyDto>> UpdateBlogReplyStatusAsync(int replyBlogId, UpdateBlogReviewStatusDto dto, CancellationToken cancellationToken = default);

    Task<Result<List<BlogCommentBanReasonDto>>> GetBlogCommentBanReasonsAsync(CancellationToken cancellationToken = default);

    Task<Result<PaginatedResponse<BlogReviewPermissionDto>>> GetBlogReviewPermissionsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? status = null,
        bool sortDesc = true,
        CancellationToken cancellationToken = default);

    Task<Result<BlogReviewPermissionDto>> UpdateBlogReviewPermissionAsync(
        int accountId,
        UpdateBlogReviewPermissionDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<ReactionSummaryDto>> ReactToBlogAsync(int blogPostId, UpsertReactionDto dto, CancellationToken cancellationToken = default);

    Task<Result<bool>> RemoveBlogReactionAsync(int blogPostId, CancellationToken cancellationToken = default);

    Task<Result<ReactionSummaryDto>> GetBlogReactionSummaryAsync(int blogPostId, CancellationToken cancellationToken = default);

    Task<Result<string?>> GetMyBlogReactionAsync(int blogPostId, CancellationToken cancellationToken = default);

    Task<Result<ReactionSummaryDto>> ReactToReviewAsync(int reviewBlogId, UpsertReactionDto dto, CancellationToken cancellationToken = default);

    Task<Result<bool>> RemoveReviewReactionAsync(int reviewBlogId, CancellationToken cancellationToken = default);

    Task<Result<ReactionSummaryDto>> GetReviewReactionSummaryAsync(int reviewBlogId, CancellationToken cancellationToken = default);

    Task<Result<string?>> GetMyReviewReactionAsync(int reviewBlogId, CancellationToken cancellationToken = default);

    Task<Result<ReactionSummaryDto>> ReactToReplyAsync(int replyBlogId, UpsertReactionDto dto, CancellationToken cancellationToken = default);

    Task<Result<bool>> RemoveReplyReactionAsync(int replyBlogId, CancellationToken cancellationToken = default);

    Task<Result<ReactionSummaryDto>> GetReplyReactionSummaryAsync(int replyBlogId, CancellationToken cancellationToken = default);

    Task<Result<string?>> GetMyReplyReactionAsync(int replyBlogId, CancellationToken cancellationToken = default);
}
