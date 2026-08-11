using System.IO;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Helpers;
using ToyStore.Application.Common.Models;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class BlogService : IBlogService
{
    private const int MaxBlogRejectReasonLength = 500;
    private const int MaxReviewCommentLength = 500;
    private const string ReactionLike = "like";
    private const string ReactionLove = "love";
    private const string ReactionHaha = "haha";
    private const string DraftStatus = "Draft";
    private const string PendingStatus = "Pending";
    private const string ApprovedStatus = "Approved";
    private const string ScheduledStatus = "Scheduled";
    private const string PublishedStatus = "Published";
    private const string RejectedStatus = "Rejected";
    private const string HiddenStatus = "Hidden";
    private const string ModerationPending = "Pending";
    private const string ModerationProcessing = "Processing";
    private const string ModerationApproved = "Approved";
    private const string ManualReviewStatus = "ManualReview";
    private const int CommentRateLimitPerMinute = 5;
    private const byte CommentViolationBanThreshold = 20;
    private const int CommentBanDurationDays = 7;
    private const string ApprovePublishNowDecision = "ApprovePublishNow";
    private const string ApproveKeepScheduleDecision = "ApproveKeepSchedule";
    private const string AdminRoleName = "Admin";
    private const string StaffRoleName = "Staff";
    private const string CustomerRoleName = "Customer";

    private static readonly HashSet<string> AllowedSubmitStatus = new(StringComparer.OrdinalIgnoreCase)
    {
        PendingStatus
    };
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IMapper _mapper;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IValidator<UpdateBlogReviewPermissionDto> _permissionValidator;
    private readonly IValidator<AiBlogGenerateRequest> _aiBlogGenerateRequestValidator;
    private readonly ILogger<BlogService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IBlogCommentModerationGateway _blogCommentModerationGateway;
    private readonly IBlogContentGenerationGateway _blogContentGenerationGateway;
    private readonly IImageUploadService _imageUploadService;

    public BlogService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDomainEventPublisher eventPublisher,
        IMapper mapper,
        INotificationDispatcher notificationDispatcher,
        IValidator<UpdateBlogReviewPermissionDto> permissionValidator,
        IValidator<AiBlogGenerateRequest> aiBlogGenerateRequestValidator,
        ILogger<BlogService> logger,
        ITimeProvider timeProvider,
        IBlogCommentModerationGateway blogCommentModerationGateway,
        IBlogContentGenerationGateway blogContentGenerationGateway,
        IImageUploadService imageUploadService)
    {
        _unitOfWork         = unitOfWork;
        _currentUserService = currentUserService;
        _eventPublisher     = eventPublisher;
        _mapper             = mapper;
        _notificationDispatcher = notificationDispatcher;
        _permissionValidator = permissionValidator;
        _aiBlogGenerateRequestValidator = aiBlogGenerateRequestValidator;
        _logger             = logger;
        _timeProvider       = timeProvider;
        _blogCommentModerationGateway = blogCommentModerationGateway;
        _blogContentGenerationGateway = blogContentGenerationGateway;
        _imageUploadService           = imageUploadService;
    }

    public async Task<Result<PaginatedResponse<BlogListDto>>> GetBlogsForAdminAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        bool featuredOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<PaginatedResponse<BlogListDto>>.Unauthorized();
        }

        return await GetPagedBlogsAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            status,
            featuredOnly,
            null,
            false,
            _currentUserService.AccountId,
            cancellationToken,
            null);
    }

    public async Task<Result<PaginatedResponse<BlogListDto>>> GetBlogsForStaffAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        bool featuredOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<PaginatedResponse<BlogListDto>>.Unauthorized();
        }

        return await GetPagedBlogsAsync(pageNumber, pageSize, sortBy, sortDesc, searchTerm, status, featuredOnly, _currentUserService.AccountId, false, null, cancellationToken);
    }

    public async Task<Result<PaginatedResponse<BlogListDto>>> SearchPublishedBlogsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedBlogsAsync(pageNumber, pageSize, sortBy, sortDesc, searchTerm, "Published", false, null, true, null, cancellationToken);
    }

    public async Task<Result<List<BlogCategoryDto>>> GetBlogCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Blogs.GetBlogCategoriesAsync(cancellationToken);
        var data = categories
            .Select(x => new BlogCategoryDto
            {
                BlogCategoryId = x.BlogCategoryId,
                BlogCategoryName = x.BlogCategoriesName
            })
            .ToList();

        return Result<List<BlogCategoryDto>>.Success(data);
    }

    public async Task<Result<BlogDetailDto>> GetBlogDetailsAsync(int blogPostId, CancellationToken cancellationToken = default)
    {
        if (blogPostId <= 0)
        {
            return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "Blog ID must be greater than 0.");
        }

        var blog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        if (blog == null)
        {
            return Result<BlogDetailDto>.NotFound("Blog", blogPostId);
        }

        if (!CanViewBlog(blog))
        {
            return Result<BlogDetailDto>.Unauthorized("You are not allowed to view this blog.");
        }

        var dto = _mapper.Map<BlogDetailDto>(blog);
        var summary = await BuildBlogSummaryAsync(blogPostId, cancellationToken);
        dto.LikeCount = summary.LikeCount;
        dto.LoveCount = summary.LoveCount;
        dto.HahaCount = summary.HahaCount;
        dto.CurrentUserReaction = summary.CurrentUserReaction;
        dto.CommentCount = await _unitOfWork.Blogs.CountApprovedReviewsByBlogIdAsync(blogPostId, cancellationToken);
        dto.TotalInteraction = dto.LikeCount + dto.CommentCount;

        return Result<BlogDetailDto>.Success(dto);
    }

    public async Task<Result<BlogDetailDto>> CreateBlogAsync(CreateBlogDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<BlogDetailDto>.Unauthorized();
        }

        var validateResult = await ValidateWriteInputAsync(dto.BlogCategoryId, dto.BlogTitle, dto.BlogContent, cancellationToken);
        if (validateResult != null)
        {
            return validateResult;
        }

        var entity = new BlogPost
        {
            AccountId = _currentUserService.AccountId,
            BlogCategoryId = dto.BlogCategoryId,
            BlogTitle = dto.BlogTitle.Trim(),
            BlogContent = dto.BlogContent.Trim(),
            BlogThumbnail = dto.BlogThumbnail?.Trim(),
            BlogAt = NormalizeBlogAtUtc(dto.BlogAt),
            Status = DraftStatus,
            Reason = null,
            ApprovedBy = null,
            IsDeleted = false,
            CreatedAt = _timeProvider.UtcNow
        };

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.Blogs.CreateAsync(entity, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var reloaded = await _unitOfWork.Blogs.GetByIdAsync(created.BlogPostId, cancellationToken);
            return Result<BlogDetailDto>.Success(_mapper.Map<BlogDetailDto>(reloaded!));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<BlogDetailDto>> UpdateBlogAsync(int blogPostId, UpdateBlogDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<BlogDetailDto>.Unauthorized();
        }

        var blog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        if (blog == null)
        {
            return Result<BlogDetailDto>.NotFound("Blog", blogPostId);
        }

        var roleName = _currentUserService.RoleName;
        var isAdmin = string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && blog.AccountId != _currentUserService.AccountId)
        {
            return Result<BlogDetailDto>.Unauthorized("You are not allowed to edit this blog.");
        }

        var wasHidden = string.Equals(blog.Status, HiddenStatus, StringComparison.OrdinalIgnoreCase);

        if (string.Equals(blog.Status, ApprovedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return await UpdateApprovedBlogAtAsync(blog, dto, cancellationToken);
        }

        var validateResult = await ValidateWriteInputAsync(dto.BlogCategoryId, dto.BlogTitle, dto.BlogContent, cancellationToken);
        if (validateResult != null)
        {
            return validateResult;
        }

        blog.BlogCategoryId = dto.BlogCategoryId!.Value;
        blog.BlogTitle = dto.BlogTitle!.Trim();
        blog.BlogContent = dto.BlogContent!.Trim();
        blog.BlogThumbnail = dto.BlogThumbnail?.Trim();
        blog.BlogAt = NormalizeBlogAtUtc(dto.BlogAt);
        blog.UpdatedAt = _timeProvider.UtcNow;

        blog.Status = DraftStatus;
        if (!wasHidden && string.Equals(dto.Status?.Trim(), PendingStatus, StringComparison.OrdinalIgnoreCase))
        {
            if (!blog.BlogAt.HasValue)
            {
                return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "BlogAt is required when submitting Draft to Pending.");
            }
            if (string.IsNullOrWhiteSpace(blog.BlogThumbnail))
            {
                return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "Thumbnail is required when submitting Draft to Pending.");
            }

            blog.Status = PendingStatus;
        }

        blog.Reason = null;
        blog.ApprovedBy = null;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Blogs.UpdateAsync(blog, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            var reloaded = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
            return Result<BlogDetailDto>.Success(_mapper.Map<BlogDetailDto>(reloaded!));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    public async Task<Result<BlogDetailDto>> SubmitBlogAsync(int blogPostId, SubmitBlogDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<BlogDetailDto>.Unauthorized();
        }

        var blog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        if (blog == null)
        {
            return Result<BlogDetailDto>.NotFound("Blog", blogPostId);
        }

        if (blog.AccountId != _currentUserService.AccountId)
        {
            return Result<BlogDetailDto>.Unauthorized("You are not allowed to submit this blog.");
        }

        var targetStatus = dto.Status?.Trim() ?? string.Empty;
        if (!AllowedSubmitStatus.Contains(targetStatus))
        {
            return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "Status must be Pending.");
        }

        if (!string.Equals(blog.Status, DraftStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Result<BlogDetailDto>.BusinessError("Only Draft blog can be submitted to Pending.");
        }

        if (!blog.BlogAt.HasValue)
        {
            return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "BlogAt is required when submitting Draft to Pending.");
        }

        if (string.IsNullOrWhiteSpace(blog.BlogThumbnail))
        {
            return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "Thumbnail is required when submitting Draft to Pending.");
        }

        var validateResult = await ValidateWriteInputAsync(blog.BlogCategoryId, blog.BlogTitle, blog.BlogContent, cancellationToken);
        if (validateResult != null)
        {
            return validateResult;
        }

        blog.Status = PendingStatus;
        blog.Reason = null;
        blog.ApprovedBy = null;

        blog.UpdatedAt = _timeProvider.UtcNow;
        await _unitOfWork.Blogs.UpdateAsync(blog, cancellationToken);

        // Notify admins that a blog is pending approval
        await _eventPublisher.PublishAsync("Blog", blogPostId.ToString(), NotificationEventTypes.ContentBlogPendingApproval,
            new { blogId = blogPostId, authorId = blog.AccountId }, CancellationToken.None);

        var updated = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        return Result<BlogDetailDto>.Success(_mapper.Map<BlogDetailDto>(updated!));
    }

    /// <summary>
    /// Thực hiện phê duyệt hoặc từ chối một bài viết Blog đang chờ kiểm duyệt (Pending).
    /// </summary>
    public async Task<Result<BlogDetailDto>> ApproveBlogAsync(int blogPostId, ApproveBlogDto dto, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra xác thực người dùng (phải là Quản trị viên/Người kiểm duyệt đã đăng nhập)
        if (_currentUserService.AccountId <= 0)
        {
            return Result<BlogDetailDto>.Unauthorized();
        }

        // 2. Tìm kiếm bài viết Blog trong cơ sở dữ liệu
        var blog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        if (blog == null)
        {
            return Result<BlogDetailDto>.NotFound("Blog", blogPostId);
        }

        // 3. Đảm bảo bài viết đang ở trạng thái Chờ duyệt (Pending). Trạng thái khác không được duyệt.
        if (!string.Equals(blog.Status, PendingStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Result<BlogDetailDto>.BusinessError("Only Pending blog can be approved or rejected.");
        }

        // 4. Lấy quyết định duyệt bài viết từ Client gửi lên
        var decision = dto.Decision?.Trim() ?? string.Empty;
        var isApprovePublishNow = string.Equals(decision, ApprovePublishNowDecision, StringComparison.OrdinalIgnoreCase);
        var isApproveKeepSchedule = string.Equals(decision, ApproveKeepScheduleDecision, StringComparison.OrdinalIgnoreCase);
        var isApprovedLegacy = string.Equals(decision, "Approved", StringComparison.OrdinalIgnoreCase);
        var isRejected = string.Equals(decision, "Rejected", StringComparison.OrdinalIgnoreCase);

        // NHÓM 1: Nếu quyết định là Chấp thuận duyệt đăng bài viết (Approve)
        if (isApprovePublishNow || isApproveKeepSchedule || isApprovedLegacy)
        {
            blog.Reason = null; // Xóa bỏ lý do từ chối cũ nếu có
            
            // Nhánh 1.1: Duyệt và xuất bản ngay lập tức
            if (isApprovePublishNow || (isApprovedLegacy && dto.PublishNow == true))
            {
                blog.Status = PublishedStatus; // Trạng thái: Đã xuất bản
                blog.BlogAt = _timeProvider.UtcNow; // Gán ngày xuất bản bằng thời điểm hiện tại
                _logger.LogInformation("Blog {BlogId} approved and published immediately by account {AccountId}.", blogPostId, _currentUserService.AccountId);
            }
            // Nhánh 1.2: Duyệt bài viết và giữ nguyên lịch hẹn giờ ở tương lai
            else if (blog.BlogAt.HasValue && blog.BlogAt.Value > _timeProvider.UtcNow)
            {
                blog.Status = ScheduledStatus; // Trạng thái: Hẹn giờ xuất bản
            }
            // Nhánh 1.3: Duyệt bài viết nhưng ngày hẹn giờ đã qua hoặc trùng hiện tại -> xuất bản ngay
            else
            {
                blog.Status = PublishedStatus; // Trạng thái: Đã xuất bản
                blog.BlogAt ??= _timeProvider.UtcNow; // Gán thời gian xuất bản nếu chưa có
                _logger.LogInformation("Blog {BlogId} approved and published immediately (BlogAt <= now).", blogPostId);
            }
        }
        // NHÓM 2: Nếu quyết định là Từ chối bài viết (Reject)
        else if (isRejected)
        {
            var rejectReason = dto.Reason?.Trim();
            
            // Kiểm tra lý do từ chối không được rỗng
            if (string.IsNullOrWhiteSpace(rejectReason))
            {
                return Result<BlogDetailDto>.ValidationFailure(new Dictionary<string, string[]>
                {
                    ["Reason"] = ["Reason is required when rejecting a blog."]
                });
            }
            
            // Kiểm tra lý do từ chối không vượt quá độ dài quy định
            if (rejectReason.Length > MaxBlogRejectReasonLength)
            {
                return Result<BlogDetailDto>.ValidationFailure(new Dictionary<string, string[]>
                {
                    ["Reason"] = [$"Reason must not exceed {MaxBlogRejectReasonLength} characters."]
                });
            }
            
            blog.Status = RejectedStatus; // Trạng thái: Đã bị từ chối
            blog.Reason = rejectReason; // Lưu lý do từ chối
        }
        // NHÓM 3: Quyết định gửi lên không hợp lệ
        else
        {
            return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "Decision must be ApprovePublishNow, ApproveKeepSchedule, Approved, or Rejected.");
        }

        blog.ApprovedBy = _currentUserService.AccountId;
        blog.UpdatedAt = _timeProvider.UtcNow;

        await _unitOfWork.Blogs.UpdateAsync(blog, cancellationToken);
        var updated = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);

        _logger.LogInformation("Blog {BlogId} reviewed by account {AccountId}. Decision: {Decision}", blogPostId, _currentUserService.AccountId, decision);
        return Result<BlogDetailDto>.Success(_mapper.Map<BlogDetailDto>(updated!));
    }

    public async Task<Result<BlogDetailDto>> PublishNowAsync(int blogPostId, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<BlogDetailDto>.Unauthorized();
        }

        var blog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        if (blog == null)
        {
            return Result<BlogDetailDto>.NotFound("Blog", blogPostId);
        }

        if (!string.Equals(blog.Status, ScheduledStatus, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(blog.Status, ApprovedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Result<BlogDetailDto>.BusinessError("Only Scheduled or Approved blog can be published now.");
        }

        blog.Status = PublishedStatus;
        blog.BlogAt = _timeProvider.UtcNow;
        blog.Reason = null;
        blog.UpdatedAt = _timeProvider.UtcNow;

        await _unitOfWork.Blogs.UpdateAsync(blog, cancellationToken);
        var updated = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        return Result<BlogDetailDto>.Success(_mapper.Map<BlogDetailDto>(updated!));
    }

    public async Task<Result<BlogDetailDto>> HideBlogAsync(int blogPostId, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<BlogDetailDto>.Unauthorized();
        }

        var blog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        if (blog == null)
        {
            return Result<BlogDetailDto>.NotFound("Blog", blogPostId);
        }

        var roleName = _currentUserService.RoleName;
        var isAdmin = string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase);
        var isStaff = string.Equals(roleName, "Staff", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && !(isStaff && blog.AccountId == _currentUserService.AccountId))
        {
            return Result<BlogDetailDto>.Unauthorized("You are not allowed to hide this blog.");
        }

        if (string.Equals(blog.Status, HiddenStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Result<BlogDetailDto>.BusinessError("Blog is already hidden.");
        }

        blog.Status = HiddenStatus;
        blog.IsFeatured = false;
        blog.Reason = null;
        blog.UpdatedAt = _timeProvider.UtcNow;

        await _unitOfWork.Blogs.HideAsync(blog, cancellationToken);
        var hiddenBlog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        return Result<BlogDetailDto>.Success(hiddenBlog != null ? _mapper.Map<BlogDetailDto>(hiddenBlog) : _mapper.Map<BlogDetailDto>(blog));
    }

    public async Task<Result<List<BlogReviewDto>>> GetBlogReviewsAsync(int blogPostId, CancellationToken cancellationToken = default)
    {
        if (blogPostId <= 0)
        {
            return Result<List<BlogReviewDto>>.Failure("VALIDATION_ERROR", "Blog ID must be greater than 0.");
        }

        var blog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        if (blog == null)
        {
            return Result<List<BlogReviewDto>>.NotFound("Blog", blogPostId);
        }

        var includeHidden = IsPrivilegedUser();
        var reviews = await _unitOfWork.Blogs.GetReviewsByBlogIdAsync(blogPostId, includeHidden, cancellationToken);
        reviews = reviews
            .Where(x => IsReviewVisibleToCurrentUser(x))
            .ToList();
        var reviewIds = reviews.Select(x => x.ReviewBlogId).ToList();
        var replies = reviewIds.Count == 0
            ? new List<ReviewBlogReply>()
            : await _unitOfWork.Blogs.GetRepliesByReviewIdsAsync(reviewIds, includeHidden, cancellationToken);
        replies = replies
            .Where(x => IsReplyVisibleToCurrentUser(x))
            .ToList();

        var reviewCounts = await _unitOfWork.Blogs.GetReviewReactionCountsByIdsAsync(reviewIds, cancellationToken);
        var replyIds = replies.Select(x => x.ReplyBlogId).ToList();
        var replyCounts = await _unitOfWork.Blogs.GetReplyReactionCountsByIdsAsync(replyIds, cancellationToken);

        Dictionary<int, string> myReviewReactions = new();
        Dictionary<int, string> myReplyReactions = new();
        if (_currentUserService.AccountId > 0)
        {
            myReviewReactions = await _unitOfWork.Blogs.GetMyReviewReactionsByIdsAsync(reviewIds, _currentUserService.AccountId, cancellationToken);
            myReplyReactions = await _unitOfWork.Blogs.GetMyReplyReactionsByIdsAsync(replyIds, _currentUserService.AccountId, cancellationToken);
        }

        var data = MapReviewThreads(reviews, replies, includeHidden, reviewCounts, replyCounts, myReviewReactions, myReplyReactions);
        return Result<List<BlogReviewDto>>.Success(data);
    }

    public async Task<Result<BlogReviewDto>> CreateBlogReviewAsync(int blogPostId, CreateBlogReviewDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<BlogReviewDto>.Unauthorized();
        }

        var comment = dto.Comment?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(comment) || comment.Length > MaxReviewCommentLength)
        {
            return Result<BlogReviewDto>.Failure("VALIDATION_ERROR", "Comment is required and must be at most 500 characters.");
        }

        var blog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        if (blog == null)
        {
            return Result<BlogReviewDto>.NotFound("Blog", blogPostId);
        }

        if (!string.Equals(blog.Status, PublishedStatus, StringComparison.OrdinalIgnoreCase) && !IsPrivilegedUser())
        {
            return Result<BlogReviewDto>.BusinessError("Only published blogs can be reviewed.");
        }

        var permission = await ValidateCommentPermissionAsync(_currentUserService.AccountId, cancellationToken);
        if (!permission.IsSuccess)
        {
            return Result<BlogReviewDto>.BusinessError(permission.ErrorMessage ?? "Commenting is temporarily unavailable.");
        }

        var entity = new ReviewBlog
        {
            BlogPostId = blogPostId,
            AccountId = _currentUserService.AccountId,
            Comment = comment,
            ModerationStatus = ModerationPending,
            RetryCount = 0,
            LastRetryAt = null,
            ManualReviewDeadline = null,
            IsDeleted = false,
            CreatedAt = _timeProvider.UtcNow
        };

        var created = await _unitOfWork.Blogs.CreateReviewAsync(entity, cancellationToken);
        var aiModerated = await _blogCommentModerationGateway.ModerateCommentAsync(created.ReviewBlogId, cancellationToken);
        if (!aiModerated)
        {
            _logger.LogWarning("AI moderation did not accept blog comment {ReviewBlogId}", created.ReviewBlogId);
        }
        _unitOfWork.Detach(created);
        var loaded = await _unitOfWork.Blogs.GetReviewByIdAsync(created.ReviewBlogId, cancellationToken);
        if (loaded == null)
        {
            return Result<BlogReviewDto>.NotFound("Review", created.ReviewBlogId);
        }

        return Result<BlogReviewDto>.Success(await MapReviewAsync(loaded, cancellationToken));
    }

    public async Task<Result<BlogReviewReplyDto>> CreateBlogReviewReplyAsync(int reviewBlogId, CreateBlogReviewReplyDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<BlogReviewReplyDto>.Unauthorized();
        }

        var comment = dto.Comment?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(comment) || comment.Length > MaxReviewCommentLength)
        {
            return Result<BlogReviewReplyDto>.Failure("VALIDATION_ERROR", "Comment is required and must be at most 500 characters.");
        }

        var validation = await ValidateBlogReviewReplyTargetAsync(reviewBlogId, dto.ParentReplyId, cancellationToken);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var permission = await ValidateCommentPermissionAsync(_currentUserService.AccountId, cancellationToken);
        if (!permission.IsSuccess)
        {
            return Result<BlogReviewReplyDto>.BusinessError(permission.ErrorMessage ?? "Commenting is temporarily unavailable.");
        }

        var entity = new ReviewBlogReply
        {
            ReviewBlogId = reviewBlogId,
            AccountId = _currentUserService.AccountId,
            ParentReplyId = dto.ParentReplyId,
            ReplyToAccountId = dto.ReplyToAccountId,
            Comment = comment,
            ModerationStatus = ModerationPending,
            RetryCount = 0,
            LastRetryAt = null,
            ManualReviewDeadline = null,
            IsDeleted = false,
            CreatedAt = _timeProvider.UtcNow
        };

        var created = await _unitOfWork.Blogs.CreateReplyAsync(entity, cancellationToken);
        var aiModerated = await _blogCommentModerationGateway.ModerateReplyAsync(created.ReplyBlogId, cancellationToken);
        if (!aiModerated)
        {
            _logger.LogWarning("AI moderation did not accept blog reply {ReplyBlogId}", created.ReplyBlogId);
        }
        _unitOfWork.Detach(created);
        var loaded = await _unitOfWork.Blogs.GetReplyByIdAsync(created.ReplyBlogId, cancellationToken);
        if (loaded == null)
        {
            return Result<BlogReviewReplyDto>.NotFound("Reply", created.ReplyBlogId);
        }

        return Result<BlogReviewReplyDto>.Success(await MapReplyAsync(loaded, cancellationToken));
    }

    public async Task<Result<BlogReviewReplyDto>> CreateStaffBlogReviewReplyAsync(int reviewBlogId, CreateBlogReviewReplyDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<BlogReviewReplyDto>.Unauthorized();
        }

        if (!IsPrivilegedUser())
        {
            return Result<BlogReviewReplyDto>.Forbidden("Only Admin or Staff can reply to blog reviews from this endpoint.");
        }

        var comment = dto.Comment?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(comment) || comment.Length > MaxReviewCommentLength)
        {
            return Result<BlogReviewReplyDto>.Failure("VALIDATION_ERROR", "Comment is required and must be at most 500 characters.");
        }

        var validation = await ValidateBlogReviewReplyTargetAsync(reviewBlogId, dto.ParentReplyId, cancellationToken);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var entity = new ReviewBlogReply
        {
            ReviewBlogId = reviewBlogId,
            AccountId = _currentUserService.AccountId,
            ParentReplyId = dto.ParentReplyId,
            ReplyToAccountId = dto.ReplyToAccountId,
            Comment = comment,
            ModerationStatus = ModerationApproved,
            RetryCount = 0,
            LastRetryAt = null,
            ManualReviewDeadline = null,
            IsDeleted = false,
            CreatedAt = _timeProvider.UtcNow
        };

        var created = await _unitOfWork.Blogs.CreateReplyAsync(entity, cancellationToken);
        _unitOfWork.Detach(created);
        var loaded = await _unitOfWork.Blogs.GetReplyByIdAsync(created.ReplyBlogId, cancellationToken);
        if (loaded == null)
        {
            return Result<BlogReviewReplyDto>.NotFound("Reply", created.ReplyBlogId);
        }

        return Result<BlogReviewReplyDto>.Success(await MapReplyAsync(loaded, cancellationToken));
    }

    public async Task<Result<bool>> RemoveBlogReviewAsync(int reviewBlogId, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<bool>.Unauthorized();
        }

        var review = await _unitOfWork.Blogs.GetReviewByIdAsync(reviewBlogId, cancellationToken);
        if (review == null)
        {
            return Result<bool>.NotFound("Review", reviewBlogId);
        }

        if (review.AccountId != _currentUserService.AccountId)
        {
            return Result<bool>.Unauthorized("You can only remove your own review.");
        }

        review.IsDeleted = true;
        review.UpdatedAt = _timeProvider.UtcNow;
        await _unitOfWork.Blogs.UpdateReviewAsync(review, cancellationToken);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Lấy danh sách phân trang các bình luận (Reviews) và phản hồi (Replies) bài viết Blog phục vụ cho trang quản trị (Admin/Moderator Moderation).
    /// </summary>
    public async Task<Result<PaginatedResponse<BlogReviewDto>>> GetBlogReviewsForManagementAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra phân quyền: Chỉ cho phép tài khoản Admin hoặc Staff truy cập quản lý
        if (!IsPrivilegedUser())
        {
            return Result<PaginatedResponse<BlogReviewDto>>.Unauthorized();
        }

        // 2. Validate giá trị phân trang đầu vào
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<BlogReviewDto>>.Failure("VALIDATION_ERROR", "Invalid pagination values.");
        }

        // 3. Truy xuất danh sách bình luận (Review) phân trang từ cơ sở dữ liệu dựa theo từ khóa và trạng thái kiểm duyệt
        var reviews = await _unitOfWork.Blogs.GetPagedReviewsForManagementAsync(pageNumber, pageSize, searchTerm, status, cancellationToken);
        var reviewIds = reviews.Select(x => x.ReviewBlogId).ToList();
        
        // 4. Truy xuất tất cả phản hồi (Reply) liên quan đến các bình luận trên
        var allReplies = reviewIds.Count == 0
            ? new List<ReviewBlogReply>()
            : await _unitOfWork.Blogs.GetRepliesByReviewIdsAsync(reviewIds, includeHidden: true, cancellationToken);
            
        // Lọc danh sách phản hồi: Chỉ lấy phản hồi chưa bị xóa, không bị ẩn và có trạng thái là chờ duyệt thủ công hoặc đã duyệt
        var replies = allReplies
            .Where(x =>
                !x.IsDeleted &&
                !x.IsHidden &&
                (string.Equals(x.ModerationStatus, ManualReviewStatus, StringComparison.OrdinalIgnoreCase)
                 || string.Equals(x.ModerationStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase)))
            .ToList();
            
        // 5. Truy xuất thông tin số lượng Like/Reaction của các bình luận và phản hồi tương ứng
        var reviewCounts = await _unitOfWork.Blogs.GetReviewReactionCountsByIdsAsync(reviewIds, cancellationToken);
        var replyIds = replies.Select(x => x.ReplyBlogId).ToList();
        var replyCounts = await _unitOfWork.Blogs.GetReplyReactionCountsByIdsAsync(replyIds, cancellationToken);
        
        // 6. Ánh xạ (Map) danh sách bình luận và phản hồi phẳng thành cấu trúc hình cây phân cấp
        var mapped = MapReviewThreads(
            reviews,
            replies,
            includeHidden: true,
            reviewCounts,
            replyCounts,
            new Dictionary<int, string>(),
            new Dictionary<int, string>());

        // 7. Duyệt qua từng bình luận và phản hồi trong danh sách kết quả để đính kèm lý do bị từ chối/bị cấm hiển thị (nếu có)
        foreach (var reviewDto in mapped)
        {
            // Lấy nhật ký từ chối duyệt bình luận gần nhất của bình luận này
            var latestRejectedLog = await _unitOfWork.Blogs.GetLatestRejectedCommentLogAsync(reviewDto.ReviewBlogId, cancellationToken);
            reviewDto.BanReasonId = latestRejectedLog?.BanReasonId;
            reviewDto.BanReasonContent = latestRejectedLog?.BanReason?.Content;

            // Làm phẳng tất cả các cấp phản hồi của bình luận này để duyệt tìm lý do từ chối tương ứng
            var replyDtos = FlattenReplies(reviewDto.Replies);
            foreach (var replyDto in replyDtos)
            {
                // Lấy nhật ký từ chối duyệt phản hồi gần nhất của phản hồi con này
                var latestRejectedReplyLog = await _unitOfWork.Blogs.GetLatestRejectedReplyLogAsync(replyDto.ReplyBlogId, cancellationToken);
                replyDto.BanReasonId = latestRejectedReplyLog?.BanReasonId;
                replyDto.BanReasonContent = latestRejectedReplyLog?.BanReason?.Content;
            }
        }

        // 8. Đếm tổng số lượng bản ghi để phân trang và trả về đối tượng kết quả hoàn chỉnh
        var totalCount = await _unitOfWork.Blogs.CountReviewsForManagementAsync(searchTerm, status, cancellationToken);
        return Result<PaginatedResponse<BlogReviewDto>>.Success(new PaginatedResponse<BlogReviewDto>(mapped, totalCount, pageNumber, pageSize));
    }

    /// <summary>
    /// Cập nhật trạng thái kiểm duyệt (Approved, Rejected, ManualReview) của một bình luận Blog (Review/Comment).
    /// </summary>
    public async Task<Result<BlogReviewDto>> UpdateBlogReviewStatusAsync(int reviewBlogId, UpdateBlogReviewStatusDto dto, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra quyền hạn: Chỉ Admin hoặc Staff có quyền kiểm duyệt bình luận
        if (!IsPrivilegedUser())
        {
            return Result<BlogReviewDto>.Unauthorized();
        }

        // 2. Tìm kiếm bình luận trong cơ sở dữ liệu
        var review = await _unitOfWork.Blogs.GetReviewByIdAsync(reviewBlogId, cancellationToken);
        if (review == null)
        {
            return Result<BlogReviewDto>.NotFound("Review", reviewBlogId);
        }

        // 3. Kiểm tra tính hợp lệ của trạng thái duyệt mới yêu cầu
        var nextStatus = dto.ModerationStatus?.Trim() ?? string.Empty;
        var isAllowedStatus =
            string.Equals(nextStatus, ManualReviewStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(nextStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(nextStatus, RejectedStatus, StringComparison.OrdinalIgnoreCase);

        if (!isAllowedStatus)
        {
            return Result<BlogReviewDto>.Failure("VALIDATION_ERROR", "ModerationStatus must be ManualReview, Approved, or Rejected.");
        }

        var isRejecting = string.Equals(nextStatus, RejectedStatus, StringComparison.OrdinalIgnoreCase);
        // 4. Ràng buộc bảo mật: Staff không thể từ chối bình luận của các tài khoản Staff/Admin khác (chỉ được từ chối bình luận của Khách hàng)
        if (isRejecting && IsStaffUser() && !IsCustomerAccount(review.Account))
        {
            return Result<BlogReviewDto>.Forbidden("You do not have permission to reject this blog review.");
        }

        // 5. Nếu từ chối bình luận, bắt buộc phải cung cấp mã lý do cấm (BanReasonId)
        if (isRejecting && !dto.BanReasonId.HasValue)
        {
            return Result<BlogReviewDto>.Failure("VALIDATION_ERROR", "BanReasonId is required when ModerationStatus is Rejected.");
        }

        // Kiểm tra mã lý do cấm có tồn tại trong danh sách lý do hợp lệ của hệ thống hay không
        if (dto.BanReasonId.HasValue)
        {
            var banReasons = await _unitOfWork.Blogs.GetBlogCommentBanReasonsAsync(cancellationToken);
            if (!banReasons.Any(x => x.BanReasonId == dto.BanReasonId.Value))
            {
                return Result<BlogReviewDto>.Failure("VALIDATION_ERROR", "BanReasonId is invalid.");
            }
        }

        // 6. Ràng buộc luồng: Một bình luận đã được duyệt hiển thị (Approved) không thể đổi trạng thái ngược về Chờ duyệt (ManualReview)
        if (string.Equals(review.ModerationStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase)
            && string.Equals(nextStatus, ManualReviewStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Result<BlogReviewDto>.Failure("VALIDATION_ERROR", "Approved review cannot be changed back to ManualReview.");
        }

        var wasRejected = string.Equals(review.ModerationStatus, RejectedStatus, StringComparison.OrdinalIgnoreCase);
        var now = _timeProvider.UtcNow;
        review.ModerationStatus = nextStatus;
        review.UpdatedAt = now;
        
        // 7. Cập nhật trạng thái bình luận vào DB
        await _unitOfWork.Blogs.UpdateReviewAsync(review, cancellationToken);

        // 8. Lưu lịch sử/nhật ký hoạt động kiểm duyệt bình luận (BlogCommentModerationLog)
        await _unitOfWork.Blogs.AddCommentModerationLogAsync(new BlogCommentModerationLog
        {
            TargetType = "Comment",
            CommentId = review.ReviewBlogId,
            ModeratorType = "Admin",
            ModeratedBy = _currentUserService.AccountId > 0 ? _currentUserService.AccountId : null,
            Action = string.Equals(nextStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase) ? "Overridden" : nextStatus,
            BanReasonId = string.Equals(nextStatus, RejectedStatus, StringComparison.OrdinalIgnoreCase) ? dto.BanReasonId : null,
            CreatedAt = now
        }, cancellationToken);

        // 9. Nếu chuyển trạng thái từ bình thường sang Bị từ chối (Rejected): Áp dụng điểm vi phạm bình luận cho tài khoản người dùng
        if (!wasRejected && isRejecting)
        {
            await ApplyCommentViolationAsync(review.AccountId, now, cancellationToken);
        }

        // 10. Gửi thông báo hệ thống/Email về trạng thái duyệt bình luận mới đến tác giả bình luận
        await SendReviewStatusNotificationAsync(review, nextStatus, dto.BanReasonId, cancellationToken);

        var updated = await _unitOfWork.Blogs.GetReviewByIdAsync(reviewBlogId, cancellationToken);
        return Result<BlogReviewDto>.Success(await MapReviewAsync(updated!, cancellationToken));
    }

    /// <summary>
    /// Cập nhật trạng thái kiểm duyệt (Approved, Rejected, ManualReview) của một phản hồi bình luận Blog (Reply).
    /// </summary>
    public async Task<Result<BlogReviewReplyDto>> UpdateBlogReplyStatusAsync(int replyBlogId, UpdateBlogReviewStatusDto dto, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra quyền hạn: Chỉ Admin hoặc Staff có quyền kiểm duyệt phản hồi bình luận
        if (!IsPrivilegedUser())
        {
            return Result<BlogReviewReplyDto>.Unauthorized();
        }

        // 2. Tìm kiếm phản hồi bình luận trong cơ sở dữ liệu
        var reply = await _unitOfWork.Blogs.GetReplyByIdAsync(replyBlogId, cancellationToken);
        if (reply == null)
        {
            return Result<BlogReviewReplyDto>.NotFound("Reply", replyBlogId);
        }

        // 3. Kiểm tra tính hợp lệ của trạng thái duyệt mới yêu cầu
        var nextStatus = dto.ModerationStatus?.Trim() ?? string.Empty;
        var isAllowedStatus =
            string.Equals(nextStatus, ManualReviewStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(nextStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(nextStatus, RejectedStatus, StringComparison.OrdinalIgnoreCase);

        if (!isAllowedStatus)
        {
            return Result<BlogReviewReplyDto>.Failure("VALIDATION_ERROR", "ModerationStatus must be ManualReview, Approved, or Rejected.");
        }

        var isRejecting = string.Equals(nextStatus, RejectedStatus, StringComparison.OrdinalIgnoreCase);
        // 4. Ràng buộc bảo mật: Staff không thể từ chối phản hồi của các tài khoản Staff/Admin khác (chỉ được từ chối phản hồi của Khách hàng)
        if (isRejecting && IsStaffUser() && !IsCustomerAccount(reply.Account))
        {
            return Result<BlogReviewReplyDto>.Forbidden("You do not have permission to reject this blog review reply.");
        }

        // 5. Nếu từ chối phản hồi, bắt buộc phải cung cấp mã lý do cấm (BanReasonId)
        if (isRejecting && !dto.BanReasonId.HasValue)
        {
            return Result<BlogReviewReplyDto>.Failure("VALIDATION_ERROR", "BanReasonId is required when ModerationStatus is Rejected.");
        }

        // Kiểm tra mã lý do cấm có tồn tại trong danh sách lý do hợp lệ của hệ thống hay không
        if (dto.BanReasonId.HasValue)
        {
            var banReasons = await _unitOfWork.Blogs.GetBlogCommentBanReasonsAsync(cancellationToken);
            if (!banReasons.Any(x => x.BanReasonId == dto.BanReasonId.Value))
            {
                return Result<BlogReviewReplyDto>.Failure("VALIDATION_ERROR", "BanReasonId is invalid.");
            }
        }

        // 6. Ràng buộc luồng: Một phản hồi đã được duyệt hiển thị (Approved) không thể đổi trạng thái ngược về Chờ duyệt (ManualReview)
        if (string.Equals(reply.ModerationStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase)
            && string.Equals(nextStatus, ManualReviewStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Result<BlogReviewReplyDto>.Failure("VALIDATION_ERROR", "Approved reply cannot be changed back to ManualReview.");
        }

        var wasRejected = string.Equals(reply.ModerationStatus, RejectedStatus, StringComparison.OrdinalIgnoreCase);
        var now = _timeProvider.UtcNow;
        reply.ModerationStatus = nextStatus;
        reply.UpdatedAt = now;
        
        // 7. Cập nhật trạng thái phản hồi vào DB
        await _unitOfWork.Blogs.UpdateReplyAsync(reply, cancellationToken);

        // 8. Lưu lịch sử/nhật ký hoạt động kiểm duyệt phản hồi (BlogCommentModerationLog)
        await _unitOfWork.Blogs.AddCommentModerationLogAsync(new BlogCommentModerationLog
        {
            TargetType = "Reply",
            ReplyId = reply.ReplyBlogId,
            ModeratorType = "Admin",
            ModeratedBy = _currentUserService.AccountId > 0 ? _currentUserService.AccountId : null,
            Action = string.Equals(nextStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase) ? "Overridden" : nextStatus,
            BanReasonId = string.Equals(nextStatus, RejectedStatus, StringComparison.OrdinalIgnoreCase) ? dto.BanReasonId : null,
            CreatedAt = now
        }, cancellationToken);

        // 9. Nếu chuyển trạng thái từ bình thường sang Bị từ chối (Rejected): Áp dụng điểm vi phạm bình luận cho tài khoản người dùng
        if (!wasRejected && isRejecting)
        {
            await ApplyCommentViolationAsync(reply.AccountId, now, cancellationToken);
        }

        // 10. Gửi thông báo hệ thống/Email về trạng thái duyệt phản hồi mới đến tác giả phản hồi
        await SendReplyStatusNotificationAsync(reply, nextStatus, dto.BanReasonId, cancellationToken);

        var updated = await _unitOfWork.Blogs.GetReplyByIdAsync(replyBlogId, cancellationToken);
        return Result<BlogReviewReplyDto>.Success(await MapReplyAsync(updated!, cancellationToken));
    }

    public async Task<Result<List<BlogCommentBanReasonDto>>> GetBlogCommentBanReasonsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsPrivilegedUser())
        {
            return Result<List<BlogCommentBanReasonDto>>.Unauthorized();
        }

        var reasons = await _unitOfWork.Blogs.GetBlogCommentBanReasonsAsync(cancellationToken);
        var mapped = reasons.Select(x => new BlogCommentBanReasonDto
        {
            BanReasonId = x.BanReasonId,
            Content = x.Content,
            CreatedAt = x.CreatedAt
        }).ToList();

        return Result<List<BlogCommentBanReasonDto>>.Success(mapped);
    }

    public async Task<Result<PaginatedResponse<BlogReviewPermissionDto>>> GetBlogReviewPermissionsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? status = null,
        bool sortDesc = true,
        CancellationToken cancellationToken = default)
    {
        if (!IsPrivilegedUser())
        {
            return Result<PaginatedResponse<BlogReviewPermissionDto>>.Unauthorized();
        }

        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<BlogReviewPermissionDto>>.Failure("VALIDATION_ERROR", "Invalid pagination values.");
        }

        if (!string.IsNullOrWhiteSpace(status)
            && !string.Equals(status, "active", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "banned", StringComparison.OrdinalIgnoreCase))
        {
            return Result<PaginatedResponse<BlogReviewPermissionDto>>.Failure("VALIDATION_ERROR", "Status must be Active or Banned.");
        }

        var accounts = await _unitOfWork.Blogs.GetPagedCustomerCommentPermissionAccountsAsync(pageNumber, pageSize, searchTerm, status, sortDesc, cancellationToken);
        var totalCount = await _unitOfWork.Blogs.CountCustomerCommentPermissionAccountsAsync(searchTerm, status, cancellationToken);
        var mapped = accounts.Select(MapPermissionFromAccount).ToList();
        foreach (var item in mapped)
        {
            NormalizePermissionDates(item);
        }

        return Result<PaginatedResponse<BlogReviewPermissionDto>>.Success(
            new PaginatedResponse<BlogReviewPermissionDto>(mapped, totalCount, pageNumber, pageSize));
    }

    public async Task<Result<BlogReviewPermissionDto>> UpdateBlogReviewPermissionAsync(
        int accountId,
        UpdateBlogReviewPermissionDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!IsPrivilegedUser())
        {
            return Result<BlogReviewPermissionDto>.Unauthorized();
        }

        if (accountId <= 0)
        {
            return Result<BlogReviewPermissionDto>.Failure("VALIDATION_ERROR", "AccountId must be greater than 0.");
        }

        var validationResult = await _permissionValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<BlogReviewPermissionDto>.ValidationFailure(errors);
        }

        var state = await _unitOfWork.Blogs.GetCommentPermissionStateAsync(accountId, cancellationToken);
        if (state == null || !state.IsCommentBanned)
        {
            return Result<BlogReviewPermissionDto>.Failure("VALIDATION_ERROR", "This account is not currently banned from blog comments.");
        }

        var now = _timeProvider.UtcNow;
        state.IsCommentBanned = false;
        state.BannedAt = null;
        state.BanExpiresAt = null;
        state.ViolationCount = 0;
        state.LastViolatedAt = null;
        state.UnbannedAt = now;
        state.UnbannedBy = _currentUserService.AccountId > 0 ? _currentUserService.AccountId : null;
        state.UpdatedAt = now;

        await _unitOfWork.Blogs.UpdateCommentPermissionStateAsync(state, cancellationToken);
        await SendBlogCommentPermissionRestoredNotificationAsync(state.AccountId, cancellationToken);

        var updated = await _unitOfWork.Blogs.GetCommentPermissionStateAsync(accountId, cancellationToken);
        var mapped = _mapper.Map<BlogReviewPermissionDto>(updated!);
        NormalizePermissionDates(mapped);
        return Result<BlogReviewPermissionDto>.Success(mapped);
    }

    public async Task<Result<ReactionSummaryDto>> ReactToBlogAsync(int blogPostId, UpsertReactionDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<ReactionSummaryDto>.Unauthorized();
        }

        var blog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        if (blog == null)
        {
            return Result<ReactionSummaryDto>.NotFound("Blog", blogPostId);
        }

        var reactionType = await ResolveReactionTypeAsync(dto.ReactionCode, cancellationToken);
        if (reactionType == null)
        {
            return Result<ReactionSummaryDto>.Failure("VALIDATION_ERROR", "Invalid reaction code.");
        }

        var existing = await _unitOfWork.Blogs.GetBlogReactionAsync(blogPostId, _currentUserService.AccountId, cancellationToken);
        if (existing != null && existing.ReactionTypeId == reactionType.ReactionTypeId)
        {
            await _unitOfWork.Blogs.RemoveBlogReactionAsync(blogPostId, _currentUserService.AccountId, cancellationToken);
            return Result<ReactionSummaryDto>.Success(await BuildBlogSummaryAsync(blogPostId, cancellationToken));
        }

        await _unitOfWork.Blogs.UpsertBlogReactionAsync(blogPostId, _currentUserService.AccountId, reactionType.ReactionTypeId, cancellationToken);
        return Result<ReactionSummaryDto>.Success(await BuildBlogSummaryAsync(blogPostId, cancellationToken));
    }

    public async Task<Result<bool>> RemoveBlogReactionAsync(int blogPostId, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<bool>.Unauthorized();
        }

        var blog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        if (blog == null)
        {
            return Result<bool>.NotFound("Blog", blogPostId);
        }

        var removed = await _unitOfWork.Blogs.RemoveBlogReactionAsync(blogPostId, _currentUserService.AccountId, cancellationToken);
        return Result<bool>.Success(removed);
    }

    public async Task<Result<ReactionSummaryDto>> GetBlogReactionSummaryAsync(int blogPostId, CancellationToken cancellationToken = default)
    {
        var blog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        if (blog == null)
        {
            return Result<ReactionSummaryDto>.NotFound("Blog", blogPostId);
        }

        return Result<ReactionSummaryDto>.Success(await BuildBlogSummaryAsync(blogPostId, cancellationToken));
    }

    public async Task<Result<string?>> GetMyBlogReactionAsync(int blogPostId, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<string?>.Unauthorized();
        }

        var blog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        if (blog == null)
        {
            return Result<string?>.NotFound("Blog", blogPostId);
        }

        var reaction = await _unitOfWork.Blogs.GetBlogReactionAsync(blogPostId, _currentUserService.AccountId, cancellationToken);
        return Result<string?>.Success(reaction?.ReactionType?.Code);
    }

    public async Task<Result<ReactionSummaryDto>> ReactToReviewAsync(int reviewBlogId, UpsertReactionDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<ReactionSummaryDto>.Unauthorized();
        }

        var review = await _unitOfWork.Blogs.GetReviewByIdAsync(reviewBlogId, cancellationToken);
        if (review == null)
        {
            return Result<ReactionSummaryDto>.NotFound("Review", reviewBlogId);
        }

        if (review.IsDeleted || !string.Equals(review.ModerationStatus, ModerationApproved, StringComparison.OrdinalIgnoreCase))
        {
            return Result<ReactionSummaryDto>.BusinessError("Cannot react to deleted or unapproved review.");
        }

        var reactionType = await ResolveReactionTypeAsync(dto.ReactionCode, cancellationToken);
        if (reactionType == null)
        {
            return Result<ReactionSummaryDto>.Failure("VALIDATION_ERROR", "Invalid reaction code.");
        }

        var existing = await _unitOfWork.Blogs.GetReviewReactionAsync(reviewBlogId, _currentUserService.AccountId, cancellationToken);
        if (existing != null && existing.ReactionTypeId == reactionType.ReactionTypeId)
        {
            await _unitOfWork.Blogs.RemoveReviewReactionAsync(reviewBlogId, _currentUserService.AccountId, cancellationToken);
            return Result<ReactionSummaryDto>.Success(await BuildReviewSummaryAsync(reviewBlogId, cancellationToken));
        }

        await _unitOfWork.Blogs.UpsertReviewReactionAsync(reviewBlogId, _currentUserService.AccountId, reactionType.ReactionTypeId, cancellationToken);
        return Result<ReactionSummaryDto>.Success(await BuildReviewSummaryAsync(reviewBlogId, cancellationToken));
    }

    public async Task<Result<bool>> RemoveReviewReactionAsync(int reviewBlogId, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<bool>.Unauthorized();
        }

        var review = await _unitOfWork.Blogs.GetReviewByIdAsync(reviewBlogId, cancellationToken);
        if (review == null)
        {
            return Result<bool>.NotFound("Review", reviewBlogId);
        }

        var removed = await _unitOfWork.Blogs.RemoveReviewReactionAsync(reviewBlogId, _currentUserService.AccountId, cancellationToken);
        return Result<bool>.Success(removed);
    }

    public async Task<Result<ReactionSummaryDto>> GetReviewReactionSummaryAsync(int reviewBlogId, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Blogs.GetReviewByIdAsync(reviewBlogId, cancellationToken);
        if (review == null)
        {
            return Result<ReactionSummaryDto>.NotFound("Review", reviewBlogId);
        }

        return Result<ReactionSummaryDto>.Success(await BuildReviewSummaryAsync(reviewBlogId, cancellationToken));
    }

    public async Task<Result<string?>> GetMyReviewReactionAsync(int reviewBlogId, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<string?>.Unauthorized();
        }

        var review = await _unitOfWork.Blogs.GetReviewByIdAsync(reviewBlogId, cancellationToken);
        if (review == null)
        {
            return Result<string?>.NotFound("Review", reviewBlogId);
        }

        var reaction = await _unitOfWork.Blogs.GetReviewReactionAsync(reviewBlogId, _currentUserService.AccountId, cancellationToken);
        return Result<string?>.Success(reaction?.ReactionType?.Code);
    }

    public async Task<Result<ReactionSummaryDto>> ReactToReplyAsync(int replyBlogId, UpsertReactionDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<ReactionSummaryDto>.Unauthorized();
        }

        var reply = await _unitOfWork.Blogs.GetReplyByIdAsync(replyBlogId, cancellationToken);
        if (reply == null)
        {
            return Result<ReactionSummaryDto>.NotFound("Reply", replyBlogId);
        }

        if (reply.IsDeleted || !string.Equals(reply.ModerationStatus, ModerationApproved, StringComparison.OrdinalIgnoreCase))
        {
            return Result<ReactionSummaryDto>.BusinessError("Cannot react to deleted or unapproved reply.");
        }

        var reactionType = await ResolveReactionTypeAsync(dto.ReactionCode, cancellationToken);
        if (reactionType == null)
        {
            return Result<ReactionSummaryDto>.Failure("VALIDATION_ERROR", "Invalid reaction code.");
        }

        var existing = await _unitOfWork.Blogs.GetReplyReactionAsync(replyBlogId, _currentUserService.AccountId, cancellationToken);
        if (existing != null && existing.ReactionTypeId == reactionType.ReactionTypeId)
        {
            await _unitOfWork.Blogs.RemoveReplyReactionAsync(replyBlogId, _currentUserService.AccountId, cancellationToken);
            return Result<ReactionSummaryDto>.Success(await BuildReplySummaryAsync(replyBlogId, cancellationToken));
        }

        await _unitOfWork.Blogs.UpsertReplyReactionAsync(replyBlogId, _currentUserService.AccountId, reactionType.ReactionTypeId, cancellationToken);
        return Result<ReactionSummaryDto>.Success(await BuildReplySummaryAsync(replyBlogId, cancellationToken));
    }

    public async Task<Result<bool>> RemoveReplyReactionAsync(int replyBlogId, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<bool>.Unauthorized();
        }

        var reply = await _unitOfWork.Blogs.GetReplyByIdAsync(replyBlogId, cancellationToken);
        if (reply == null)
        {
            return Result<bool>.NotFound("Reply", replyBlogId);
        }

        var removed = await _unitOfWork.Blogs.RemoveReplyReactionAsync(replyBlogId, _currentUserService.AccountId, cancellationToken);
        return Result<bool>.Success(removed);
    }

    public async Task<Result<ReactionSummaryDto>> GetReplyReactionSummaryAsync(int replyBlogId, CancellationToken cancellationToken = default)
    {
        var reply = await _unitOfWork.Blogs.GetReplyByIdAsync(replyBlogId, cancellationToken);
        if (reply == null)
        {
            return Result<ReactionSummaryDto>.NotFound("Reply", replyBlogId);
        }

        return Result<ReactionSummaryDto>.Success(await BuildReplySummaryAsync(replyBlogId, cancellationToken));
    }

    public async Task<Result<string?>> GetMyReplyReactionAsync(int replyBlogId, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<string?>.Unauthorized();
        }

        var reply = await _unitOfWork.Blogs.GetReplyByIdAsync(replyBlogId, cancellationToken);
        if (reply == null)
        {
            return Result<string?>.NotFound("Reply", replyBlogId);
        }

        var reaction = await _unitOfWork.Blogs.GetReplyReactionAsync(replyBlogId, _currentUserService.AccountId, cancellationToken);
        return Result<string?>.Success(reaction?.ReactionType?.Code);
    }

    private async Task<Result<BlogDetailDto>> UpdateApprovedBlogAtAsync(
        BlogPost blog,
        UpdateBlogDto dto,
        CancellationToken cancellationToken)
    {
        if (!dto.BlogAt.HasValue)
        {
            return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "BlogAt is required when blog is Approved.");
        }

        var normalizedBlogAtUtc = _timeProvider.ToUtc(dto.BlogAt.Value);
        if (normalizedBlogAtUtc <= _timeProvider.UtcNow)
        {
            return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "BlogAt must be in the future.");
        }

        blog.BlogAt = normalizedBlogAtUtc;
        blog.Status = ScheduledStatus;
        blog.Reason = null;
        blog.UpdatedAt = _timeProvider.UtcNow;

        await _unitOfWork.Blogs.UpdateAsync(blog, cancellationToken);
        var updated = await _unitOfWork.Blogs.GetByIdAsync(blog.BlogPostId, cancellationToken);
        return Result<BlogDetailDto>.Success(_mapper.Map<BlogDetailDto>(updated!));
    }

    private async Task<Result<PaginatedResponse<BlogListDto>>> GetPagedBlogsAsync(
        int pageNumber,
        int pageSize,
        string? sortBy,
        bool sortDesc,
        string? searchTerm,
        string? status,
        bool featuredOnly,
        int? createdByAccountId,
        bool onlyPublished,
        int? adminSelfVisibleAccountId,
        CancellationToken cancellationToken,
        IReadOnlyCollection<string>? allowedStatuses = null)
    {
        if (pageNumber < 1)
        {
            return Result<PaginatedResponse<BlogListDto>>.Failure("VALIDATION_ERROR", "Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<BlogListDto>>.Failure("VALIDATION_ERROR", "Page size must be between 1 and 100.");
        }

        var items = await _unitOfWork.Blogs.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            status,
            featuredOnly,
            createdByAccountId,
            onlyPublished,
            adminSelfVisibleAccountId,
            allowedStatuses,
            cancellationToken);

        var totalCount = await _unitOfWork.Blogs.CountAsync(searchTerm, status, featuredOnly, createdByAccountId, onlyPublished, adminSelfVisibleAccountId, allowedStatuses, cancellationToken);
        var mapped = _mapper.Map<List<BlogListDto>>(items);
        return Result<PaginatedResponse<BlogListDto>>.Success(new PaginatedResponse<BlogListDto>(mapped, totalCount, pageNumber, pageSize));
    }

    private async Task<Result<BlogDetailDto>?> ValidateWriteInputAsync(
        short? blogCategoryId,
        string? blogTitle,
        string? blogContent,
        CancellationToken cancellationToken)
    {
        if (!blogCategoryId.HasValue || blogCategoryId.Value <= 0)
        {
            return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "Blog category is required.");
        }

        if (string.IsNullOrWhiteSpace(blogTitle))
        {
            return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "Blog title is required.");
        }

        if (string.IsNullOrWhiteSpace(blogContent))
        {
            return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "Blog content is required.");
        }

        var categoryExists = await _unitOfWork.Blogs.BlogCategoryExistsAsync(blogCategoryId.Value, cancellationToken);
        if (!categoryExists)
        {
            return Result<BlogDetailDto>.NotFound("Blog category", blogCategoryId.Value);
        }

        return null;
    }

    private bool CanViewBlog(BlogPost blog)
    {
        if (string.Equals(blog.Status, DraftStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(blog.Status, RejectedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return _currentUserService.AccountId > 0
                && blog.AccountId == _currentUserService.AccountId;
        }

        if (string.Equals(blog.Status, HiddenStatus, StringComparison.OrdinalIgnoreCase))
        {
            if (_currentUserService.AccountId <= 0)
            {
                return false;
            }

            if (blog.AccountId == _currentUserService.AccountId)
            {
                return true;
            }

            var hiddenRoleName = _currentUserService.RoleName;
            return string.Equals(hiddenRoleName, "Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(hiddenRoleName, "Staff", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(blog.Status, "Published", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (_currentUserService.AccountId <= 0)
        {
            return false;
        }

        if (blog.AccountId == _currentUserService.AccountId)
        {
            return true;
        }

        var roleName = _currentUserService.RoleName;
        return string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private DateTime? NormalizeBlogAtUtc(DateTime? blogAt)
    {
        if (!blogAt.HasValue)
        {
            return null;
        }

        return _timeProvider.ToUtc(blogAt.Value);
    }


    private bool IsPrivilegedUser()
    {
        return IsAdminUser() || IsStaffUser();
    }

    private bool IsAdminUser()
    {
        return string.Equals(_currentUserService.RoleName, AdminRoleName, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsStaffUser()
    {
        return string.Equals(_currentUserService.RoleName, StaffRoleName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCustomerAccount(Account? account)
    {
        return string.Equals(account?.Role?.RoleName, CustomerRoleName, StringComparison.OrdinalIgnoreCase);
    }

    private static List<BlogReviewDto> MapReviewThreads(
        List<ReviewBlog> reviews,
        List<ReviewBlogReply> replies,
        bool includeHidden,
        Dictionary<int, Dictionary<string, int>> reviewCounts,
        Dictionary<int, Dictionary<string, int>> replyCounts,
        Dictionary<int, string> myReviewReactions,
        Dictionary<int, string> myReplyReactions)
    {
        var replyDtoLookup = replies.ToDictionary(
            x => x.ReplyBlogId,
            x => MapReply(
                x,
                replyCounts.TryGetValue(x.ReplyBlogId, out var countMap) ? countMap : null,
                myReplyReactions.TryGetValue(x.ReplyBlogId, out var myReaction) ? myReaction : null));
        foreach (var dto in replyDtoLookup.Values)
        {
            dto.Replies = new List<BlogReviewReplyDto>();
        }

        foreach (var reply in replies)
        {
            if (reply.ParentReplyId.HasValue
                && replyDtoLookup.TryGetValue(reply.ParentReplyId.Value, out var parent)
                && replyDtoLookup.TryGetValue(reply.ReplyBlogId, out var current))
            {
                parent.Replies.Add(current);
            }
        }

        var rootsByReview = replies
            .Where(x => !x.ParentReplyId.HasValue)
            .GroupBy(x => x.ReviewBlogId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => replyDtoLookup[x.ReplyBlogId]).OrderBy(x => x.CreatedAt).ToList());

        return reviews
            .Where(x => includeHidden || !x.IsDeleted)
            .Select(x =>
            {
                var mapped = MapReview(
                    x,
                    reviewCounts.TryGetValue(x.ReviewBlogId, out var countMap) ? countMap : null,
                    myReviewReactions.TryGetValue(x.ReviewBlogId, out var myReaction) ? myReaction : null);
                mapped.Replies = rootsByReview.TryGetValue(x.ReviewBlogId, out var rootReplies)
                    ? rootReplies
                    : new List<BlogReviewReplyDto>();
                return mapped;
            })
            .ToList();
    }

    private static BlogReviewDto MapReview(ReviewBlog review, Dictionary<string, int>? counts = null, string? currentUserReaction = null)
    {
        return new BlogReviewDto
        {
            ReviewBlogId = review.ReviewBlogId,
            BlogPostId = review.BlogPostId,
            BlogTitle = review.BlogPost?.BlogTitle ?? string.Empty,
            AccountId = review.AccountId,
            AccountName = review.Account?.AccountName ?? string.Empty,
            AccountRoleName = review.Account?.Role?.RoleName ?? string.Empty,
            AccountImageUrl = review.Account?.ImageUrl,
            Comment = review.Comment ?? string.Empty,
            Status = review.IsDeleted ? "Hidden" : "Visible",
            ModerationStatus = review.ModerationStatus,
            IsHidden = review.IsHidden,
            CanReply = CanReplyToModeratedContent(review.ModerationStatus, review.IsDeleted, review.IsHidden),
            LikeCount = GetReactionCount(counts, ReactionLike),
            LoveCount = GetReactionCount(counts, ReactionLove),
            HahaCount = GetReactionCount(counts, ReactionHaha),
            CurrentUserReaction = currentUserReaction,
            CreatedAt = AsUtc(review.CreatedAt),
            UpdatedAt = AsUtc(review.UpdatedAt)
        };
    }

    private async Task<BlogReviewDto> MapReviewAsync(ReviewBlog review, CancellationToken cancellationToken)
    {
        var dto = MapReview(review);
        var latestRejectedLog = await _unitOfWork.Blogs.GetLatestRejectedCommentLogAsync(review.ReviewBlogId, cancellationToken);
        dto.BanReasonId = latestRejectedLog?.BanReasonId;
        dto.BanReasonContent = latestRejectedLog?.BanReason?.Content;
        return dto;
    }

    private static BlogReviewReplyDto MapReply(ReviewBlogReply reply, Dictionary<string, int>? counts = null, string? currentUserReaction = null)
    {
        return new BlogReviewReplyDto
        {
            ReplyBlogId = reply.ReplyBlogId,
            ReviewBlogId = reply.ReviewBlogId,
            AccountId = reply.AccountId,
            AccountName = reply.Account?.AccountName ?? string.Empty,
            AccountRoleName = reply.Account?.Role?.RoleName ?? string.Empty,
            AccountImageUrl = reply.Account?.ImageUrl,
            ParentReplyId = reply.ParentReplyId,
            ReplyToAccountId = reply.ReplyToAccountId,
            ReplyToAccountName = reply.ReplyToAccount?.AccountName,
            Comment = reply.Comment,
            Status = reply.IsDeleted ? "Hidden" : "Visible",
            ModerationStatus = reply.ModerationStatus,
            CanReply = CanReplyToModeratedContent(reply.ModerationStatus, reply.IsDeleted, reply.IsHidden),
            LikeCount = GetReactionCount(counts, ReactionLike),
            LoveCount = GetReactionCount(counts, ReactionLove),
            HahaCount = GetReactionCount(counts, ReactionHaha),
            CurrentUserReaction = currentUserReaction,
            CreatedAt = AsUtc(reply.CreatedAt),
            UpdatedAt = AsUtc(reply.UpdatedAt)
        };
    }

    private static DateTime AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static DateTime? AsUtc(DateTime? value)
    {
        return value.HasValue ? AsUtc(value.Value) : null;
    }

    private static BlogReviewPermissionDto MapPermissionFromAccount(Account account)
    {
        var state = account.BlogCommentViolationCountAccount;
        return new BlogReviewPermissionDto
        {
            AccountId = account.AccountId,
            AccountName = account.AccountName,
            Email = account.Email,
            AccountImageUrl = account.ImageUrl,
            ViolationCount = state?.ViolationCount ?? 0,
            IsCommentBanned = state?.IsCommentBanned ?? false,
            BannedAt = state?.BannedAt,
            BanExpiresAt = state?.BanExpiresAt,
            UnbannedAt = state?.UnbannedAt,
            UnbannedBy = state?.UnbannedBy,
            UnbannedByName = state?.UnbannedByNavigation?.AccountName,
            LastViolatedAt = state?.LastViolatedAt,
            UpdatedAt = state?.UpdatedAt
        };
    }

    private static void NormalizePermissionDates(BlogReviewPermissionDto dto)
    {
        dto.BannedAt = AsUtc(dto.BannedAt);
        dto.BanExpiresAt = AsUtc(dto.BanExpiresAt);
        dto.UnbannedAt = AsUtc(dto.UnbannedAt);
        dto.LastViolatedAt = AsUtc(dto.LastViolatedAt);
        dto.UpdatedAt = AsUtc(dto.UpdatedAt);
    }

    private async Task<BlogReviewReplyDto> MapReplyAsync(ReviewBlogReply reply, CancellationToken cancellationToken)
    {
        var dto = MapReply(reply);
        var latestRejectedLog = await _unitOfWork.Blogs.GetLatestRejectedReplyLogAsync(reply.ReplyBlogId, cancellationToken);
        dto.BanReasonId = latestRejectedLog?.BanReasonId;
        dto.BanReasonContent = latestRejectedLog?.BanReason?.Content;
        return dto;
    }

    /// <summary>
    /// Gửi thông báo hệ thống đến tác giả bình luận (Review) khi trạng thái duyệt thay đổi.
    /// </summary>
    private async Task SendReviewStatusNotificationAsync(
        ReviewBlog review,
        string moderationStatus,
        byte? banReasonId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Bỏ qua nếu tác giả bình luận không hợp lệ
            if (review.AccountId <= 0)
            {
                return;
            }

            var title = "Comment status updated";
            var message = $"Your comment status has been updated to {moderationStatus}.";
            
            // Nếu bình luận bị từ chối (Rejected), tìm lý do cấm tương ứng để gửi kèm thông báo
            if (string.Equals(moderationStatus, RejectedStatus, StringComparison.OrdinalIgnoreCase))
            {
                var reason = string.Empty;
                if (banReasonId.HasValue)
                {
                    var reasons = await _unitOfWork.Blogs.GetBlogCommentBanReasonsAsync(cancellationToken);
                    reason = reasons.FirstOrDefault(x => x.BanReasonId == banReasonId.Value)?.Content ?? string.Empty;
                }

                title = "Comment status updated";
                message = string.IsNullOrWhiteSpace(reason)
                    ? "Your comment has been rejected."
                    : $"Your comment has been rejected because: {reason}.";
            }

            // Thực hiện điều phối thông báo chuông (Bell) đến tài khoản người dùng
            await _notificationDispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = review.AccountId,
                RecipientType = RecipientTypes.Customer,
                NotificationType = NotificationTypes.System,
                Title = title,
                Message = message,
                SendBell = true, // Gửi chuông thông báo trên website
                SendEmail = false,
                ActionTarget = $"/blog/{review.BlogPostId}", // Đường dẫn điều hướng đến bài viết khi click thông báo
                IdempotencyKey = $"blog-review-moderation:{review.ReviewBlogId}:{review.AccountId}:{moderationStatus}:{DateTime.UtcNow.Ticks}"
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            // Bọc try-catch tránh làm lỗi luồng nghiệp vụ duyệt nếu hệ thống thông báo lỗi kết nối
            _logger.LogWarning(ex, "Failed to send blog review moderation notification for review {ReviewBlogId}", review.ReviewBlogId);
        }
    }

    /// <summary>
    /// Gửi thông báo hệ thống đến tác giả phản hồi bình luận (Reply) khi trạng thái duyệt thay đổi.
    /// </summary>
    private async Task SendReplyStatusNotificationAsync(
        ReviewBlogReply reply,
        string moderationStatus,
        byte? banReasonId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Bỏ qua nếu tác giả phản hồi không hợp lệ
            if (reply.AccountId <= 0)
            {
                return;
            }

            var title = "Reply status updated";
            var message = $"Your reply status has been updated to {moderationStatus}.";
            
            // Nếu phản hồi bị từ chối, lấy lý do cấm tương ứng
            if (string.Equals(moderationStatus, RejectedStatus, StringComparison.OrdinalIgnoreCase))
            {
                var reason = string.Empty;
                if (banReasonId.HasValue)
                {
                    var reasons = await _unitOfWork.Blogs.GetBlogCommentBanReasonsAsync(cancellationToken);
                    reason = reasons.FirstOrDefault(x => x.BanReasonId == banReasonId.Value)?.Content ?? string.Empty;
                }

                title = "Reply status updated";
                message = string.IsNullOrWhiteSpace(reason)
                    ? "Your reply has been rejected."
                    : $"Your reply has been rejected because: {reason}.";
            }

            // Tìm BlogPostId tương ứng để định tuyến chính xác
            var blogPostId = reply.ReviewBlog?.BlogPostId ?? 0;
            if (blogPostId <= 0 && reply.ReviewBlogId > 0)
            {
                var review = await _unitOfWork.Blogs.GetReviewByIdAsync(reply.ReviewBlogId, cancellationToken);
                blogPostId = review?.BlogPostId ?? 0;
            }

            // Tạo thẻ neo để trình duyệt tự động scroll đến đúng vị trí phản hồi trên giao diện khi người dùng nhấp vào thông báo
            var actionTarget = blogPostId > 0
                ? $"/blog/{blogPostId}#reply-{reply.ReplyBlogId}"
                : "/blog";

            // Thực hiện điều phối thông báo chuông
            await _notificationDispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = reply.AccountId,
                RecipientType = RecipientTypes.Customer,
                NotificationType = NotificationTypes.System,
                Title = title,
                Message = message,
                SendBell = true,
                SendEmail = false,
                ActionTarget = actionTarget,
                IdempotencyKey = $"blog-reply-moderation:{reply.ReplyBlogId}:{reply.AccountId}:{moderationStatus}:{DateTime.UtcNow.Ticks}"
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send blog reply moderation notification for reply {ReplyBlogId}", reply.ReplyBlogId);
        }
    }

    /// <summary>
    /// Gửi thông báo hệ thống đến người dùng khi quyền bình luận của họ được khôi phục thành công.
    /// </summary>
    private async Task SendBlogCommentPermissionRestoredNotificationAsync(int accountId, CancellationToken cancellationToken)
    {
        try
        {
            if (accountId <= 0)
            {
                return;
            }

            // Gửi thông báo dạng chuông để người dùng biết họ đã có thể bình luận lại
            await _notificationDispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = accountId,
                RecipientType = RecipientTypes.Customer,
                NotificationType = NotificationTypes.System,
                Title = "Blog comment permission restored",
                Message = "Your blog comment permission has been restored.",
                SendBell = true,
                SendEmail = false,
                ActionTarget = "/blog",
                IdempotencyKey = $"blog-comment-permission-restored:{accountId}:{DateTime.UtcNow.Ticks}"
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send blog comment permission restored notification for account {AccountId}", accountId);
        }
    }

    private static List<BlogReviewReplyDto> FlattenReplies(IEnumerable<BlogReviewReplyDto> replies)
    {
        var result = new List<BlogReviewReplyDto>();
        foreach (var reply in replies)
        {
            result.Add(reply);
            if (reply.Replies.Count > 0)
            {
                result.AddRange(FlattenReplies(reply.Replies));
            }
        }

        return result;
    }

    private async Task<ReactionSummaryDto> BuildBlogSummaryAsync(int blogPostId, CancellationToken cancellationToken)
    {
        var counts = await _unitOfWork.Blogs.GetBlogReactionCountsAsync(blogPostId, cancellationToken);
        string? current = null;
        if (_currentUserService.AccountId > 0)
        {
            var myReaction = await _unitOfWork.Blogs.GetBlogReactionAsync(blogPostId, _currentUserService.AccountId, cancellationToken);
            current = myReaction?.ReactionType?.Code;
        }

        return BuildSummary(counts, current);
    }

    private async Task<ReactionSummaryDto> BuildReviewSummaryAsync(int reviewBlogId, CancellationToken cancellationToken)
    {
        var counts = await _unitOfWork.Blogs.GetReviewReactionCountsAsync(reviewBlogId, cancellationToken);
        string? current = null;
        if (_currentUserService.AccountId > 0)
        {
            var myReaction = await _unitOfWork.Blogs.GetReviewReactionAsync(reviewBlogId, _currentUserService.AccountId, cancellationToken);
            current = myReaction?.ReactionType?.Code;
        }

        return BuildSummary(counts, current);
    }

    private async Task<ReactionSummaryDto> BuildReplySummaryAsync(int replyBlogId, CancellationToken cancellationToken)
    {
        var counts = await _unitOfWork.Blogs.GetReplyReactionCountsAsync(replyBlogId, cancellationToken);
        string? current = null;
        if (_currentUserService.AccountId > 0)
        {
            var myReaction = await _unitOfWork.Blogs.GetReplyReactionAsync(replyBlogId, _currentUserService.AccountId, cancellationToken);
            current = myReaction?.ReactionType?.Code;
        }

        return BuildSummary(counts, current);
    }

    private static ReactionSummaryDto BuildSummary(Dictionary<string, int>? counts, string? currentUserReaction)
    {
        var like = GetReactionCount(counts, ReactionLike);
        var love = GetReactionCount(counts, ReactionLove);
        var haha = GetReactionCount(counts, ReactionHaha);

        return new ReactionSummaryDto
        {
            LikeCount = like,
            LoveCount = love,
            HahaCount = haha,
            TotalCount = like + love + haha,
            CurrentUserReaction = currentUserReaction
        };
    }

    private static int GetReactionCount(Dictionary<string, int>? counts, string code)
    {
        if (counts == null)
        {
            return 0;
        }

        return counts.TryGetValue(code, out var value) ? value : 0;
    }

    private async Task<ReactionType?> ResolveReactionTypeAsync(string reactionCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reactionCode))
        {
            return null;
        }

        var normalized = reactionCode.Trim().ToLowerInvariant();
        if (normalized != ReactionLike && normalized != ReactionLove && normalized != ReactionHaha)
        {
            return null;
        }

        return await _unitOfWork.Blogs.GetReactionTypeByCodeAsync(normalized, cancellationToken);
    }

    private bool IsReviewVisibleToCurrentUser(ReviewBlog review)
    {
        if (IsPrivilegedUser())
        {
            return true;
        }

        var isOwner = _currentUserService.AccountId > 0 && review.AccountId == _currentUserService.AccountId;
        if (string.Equals(review.ModerationStatus, ModerationApproved, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return isOwner && IsOwnerVisibleModerationStatus(review.ModerationStatus);
    }

    private bool IsReplyVisibleToCurrentUser(ReviewBlogReply reply)
    {
        if (IsPrivilegedUser())
        {
            return true;
        }

        var isOwner = _currentUserService.AccountId > 0 && reply.AccountId == _currentUserService.AccountId;
        if (string.Equals(reply.ModerationStatus, ModerationApproved, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return isOwner && IsOwnerVisibleModerationStatus(reply.ModerationStatus);
    }

    private static bool IsOwnerVisibleModerationStatus(string moderationStatus)
    {
        return string.Equals(moderationStatus, ModerationPending, StringComparison.OrdinalIgnoreCase)
            || string.Equals(moderationStatus, ModerationProcessing, StringComparison.OrdinalIgnoreCase)
            || string.Equals(moderationStatus, ManualReviewStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanReplyToModeratedContent(string moderationStatus, bool isDeleted, bool isHidden)
    {
        return !isDeleted
            && !isHidden
            && string.Equals(moderationStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Result<BlogReviewReplyDto>> ValidateBlogReviewReplyTargetAsync(
        int reviewBlogId,
        int? parentReplyId,
        CancellationToken cancellationToken)
    {
        var review = await _unitOfWork.Blogs.GetReviewByIdAsync(reviewBlogId, cancellationToken);
        if (review == null)
        {
            return Result<BlogReviewReplyDto>.NotFound("Review", reviewBlogId);
        }

        if (review.IsDeleted || review.IsHidden)
        {
            return Result<BlogReviewReplyDto>.BusinessError("Cannot reply to hidden review.");
        }

        if (!string.Equals(review.ModerationStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Result<BlogReviewReplyDto>.BusinessError("Only approved review can be replied.");
        }

        if (!parentReplyId.HasValue)
        {
            return Result<BlogReviewReplyDto>.Success(new BlogReviewReplyDto());
        }

        var parentReply = await _unitOfWork.Blogs.GetReplyByIdAsync(parentReplyId.Value, cancellationToken);
        if (parentReply == null || parentReply.ReviewBlogId != reviewBlogId)
        {
            return Result<BlogReviewReplyDto>.Failure("VALIDATION_ERROR", "Parent reply is invalid.");
        }

        if (!string.Equals(parentReply.ModerationStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Result<BlogReviewReplyDto>.BusinessError("Only approved reply can be replied.");
        }

        return Result<BlogReviewReplyDto>.Success(new BlogReviewReplyDto());
    }

    private async Task<Result<bool>> ValidateCommentPermissionAsync(int accountId, CancellationToken cancellationToken)
    {
        var now = _timeProvider.UtcNow;

        var (isLocked, _) = await _unitOfWork.Blogs.CheckAndRefreshCommentLockAsync(accountId, now, cancellationToken);
        if (isLocked)
        {
            return Result<bool>.BusinessError("Tài khoản đang bị khóa comment");
        }

        var isRateLimited = await _unitOfWork.Blogs.IncrementRateAndCheckCommentLimitAsync(
            accountId,
            now,
            CommentRateLimitPerMinute,
            windowMinutes: 1,
            cancellationToken);
        if (isRateLimited)
        {
            return Result<bool>.BusinessError("Please wait before posting another comment.");
        }

        return Result<bool>.Success(true);
    }

    private async Task ApplyCommentViolationAsync(int accountId, DateTime now, CancellationToken cancellationToken)
    {
        if (accountId <= 0)
        {
            return;
        }

        // Ensure state row exists before updating violation counters.
        await _unitOfWork.Blogs.CheckAndRefreshCommentLockAsync(accountId, now, cancellationToken);
        var state = await _unitOfWork.Blogs.GetCommentPermissionStateAsync(accountId, cancellationToken);
        if (state == null)
        {
            return;
        }

        state.ViolationCount = state.ViolationCount >= byte.MaxValue
            ? byte.MaxValue
            : (byte)(state.ViolationCount + 1);
        state.LastViolatedAt = now;
        state.UpdatedAt = now;

        if (state.ViolationCount >= CommentViolationBanThreshold)
        {
            if (!state.IsCommentBanned)
            {
                state.IsCommentBanned = true;
                state.BannedAt = now;
            }

            state.BanExpiresAt = now.AddDays(CommentBanDurationDays);
            state.UnbannedAt = null;
            state.UnbannedBy = null;
        }

        await _unitOfWork.Blogs.UpdateCommentPermissionStateAsync(state, cancellationToken);
    }

    // ═══════════════════════════════════════════════════════════════════
    // AI BLOG GENERATION
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gửi yêu cầu sinh tự động nội dung bài viết Blog bằng AI tích hợp.
    /// </summary>
    public async Task<Result<AiBlogGenerateResult>> GenerateWithAiAsync(
        AiBlogGenerateRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate định dạng cú pháp cơ bản đầu vào của request bằng FluentValidation
        var validationResult = await _aiBlogGenerateRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult<AiBlogGenerateResult>();
        }

        // 2. Validate nội dung có ý nghĩa (chống spam văn bản vô nghĩa hoặc gõ phím linh tinh)
        var meaningfulErrors = BlogMeaningfulInputHelper.ValidateMeaningfulInputs(
            request.Title,
            request.PromptStructure,
            request.Description);

        if (meaningfulErrors.Count > 0)
        {
            return Result<AiBlogGenerateResult>.ValidationFailure(meaningfulErrors);
        }

        var action = string.IsNullOrWhiteSpace(request.Action) ? "Generate" : request.Action.Trim();
        var tone   = string.IsNullOrWhiteSpace(request.DefaultTone) ? "Friendly" : request.DefaultTone.Trim();

        // 3. Chuẩn bị nội dung bài viết nguồn (Source Content) để làm ngữ cảnh tham khảo cho AI
        string? sourceContent = string.IsNullOrWhiteSpace(request.SourceContent)
            ? null
            : request.SourceContent.Trim();

        // Nếu client cung cấp ID bài viết cũ và không truyền text nguồn trực tiếp, tự động tải nội dung cũ từ DB làm context nguồn
        if (request.BlogPostId.HasValue && request.BlogPostId.Value > 0 && string.IsNullOrWhiteSpace(sourceContent))
        {
            var existingResult = await GetBlogDetailsAsync(request.BlogPostId.Value, cancellationToken);
            if (!existingResult.IsSuccess || existingResult.Data == null)
            {
                return Result<AiBlogGenerateResult>.NotFound("Blog", request.BlogPostId.Value);
            }

            sourceContent = existingResult.Data.BlogContent;
        }

        // 4. Gửi yêu cầu sinh nội dung qua AI Content Generation Gateway (đóng vai trò Client gọi API Python AI)
        var aiResult = await _blogContentGenerationGateway.GenerateAsync(
            new PythonBlogGenerateRequest
            {
                Action            = action,
                Title             = request.Title.Trim(),
                Description       = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                PromptStructure   = request.PromptStructure.Trim(),
                DefaultTone       = tone,
                DefaultCategoryId = request.DefaultCategoryId,
                SourceContent     = sourceContent
            },
            cancellationToken);

        // KIỂM TRA 1: Trường hợp API AI trả về lỗi
        if (!aiResult.IsSuccess)
        {
            // Trả lỗi chi tiết nếu gateway nhận diện thân lỗi cụ thể từ Server AI
            if (aiResult.ErrorBody != null)
            {
                _logger.LogWarning("AI generation failed with error body. Status: {Status}", aiResult.StatusCode);
                return Result<AiBlogGenerateResult>.Failure("AI_GENERATE_ERROR", "AI generation failed.");
            }

            _logger.LogWarning("AI service unavailable: {Message}", aiResult.ErrorMessage);
            return Result<AiBlogGenerateResult>.BadGateway(aiResult.ErrorMessage ?? "Unable to call AI service.");
        }

        var aiGenerated = aiResult.Data;

        // KIỂM TRA 2: Trường hợp AI phát hiện và chặn nội dung do vi phạm quy tắc chính sách
        if (aiGenerated?.IsBlocked == true)
        {
            _logger.LogWarning("AI blocked content generation for title: {Title}", request.Title);
            return Result<AiBlogGenerateResult>.BusinessError("AI blocked this content due to policy violation.");
        }

        // KIỂMTRA 3: Trường hợp AI trả về nội dung trống
        if (aiGenerated == null || string.IsNullOrWhiteSpace(aiGenerated.Content))
        {
            _logger.LogWarning("AI returned empty content for title: {Title}", request.Title);
            return Result<AiBlogGenerateResult>.Failure("AI_GENERATE_EMPTY", "AI returned empty content.");
        }

        // Lấy tiêu đề sinh ra từ AI, nếu AI không cập nhật tiêu đề mới thì giữ nguyên tiêu đề ban đầu
        var generatedTitle = string.IsNullOrWhiteSpace(aiGenerated.Title)
            ? request.Title.Trim()
            : aiGenerated.Title.Trim();

        _logger.LogInformation("AI blog generated successfully for title: {Title}", request.Title);

        // Trả về DTO kết quả sinh nội dung bài viết thành công
        return Result<AiBlogGenerateResult>.Success(new AiBlogGenerateResult
        {
            BlogPostId    = request.BlogPostId.GetValueOrDefault(0),
            Title         = generatedTitle,
            BlogContent   = aiGenerated.Content!,
            BlogCategoryId = request.DefaultCategoryId,
            PromptData    = request.PromptStructure,
            AiStatus      = "Success",
            AiError       = null
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    // BLOG THUMBNAIL UPLOAD
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Upload thumbnail blog lên Cloudinary sau khi validate extension và MIME type.
    /// </summary>
    public async Task<Result<UploadBlogThumbnailResponse>> UploadBlogThumbnailAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        const string ThumbnailFolder = "SEP490_Blogs";

        // ── Validate extension ──
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
        {
            return Result<UploadBlogThumbnailResponse>.ValidationFailure(
                new Dictionary<string, string[]>
                {
                    ["file"] = ["Only JPG, JPEG, PNG, WEBP are supported."]
                });
        }

        // ── Validate MIME type ──
        var allowedMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        if (string.IsNullOrWhiteSpace(contentType) || !allowedMimeTypes.Contains(contentType))
        {
            return Result<UploadBlogThumbnailResponse>.ValidationFailure(
                new Dictionary<string, string[]>
                {
                    ["file"] = ["Only image/jpeg, image/png, image/webp are supported."]
                });
        }

        // ── Validate kích thước file ──
        if (fileStream.Length > MaxFileSizeBytes)
        {
            return Result<UploadBlogThumbnailResponse>.ValidationFailure(
                new Dictionary<string, string[]>
                {
                    ["file"] = ["File size must not exceed 5 MB."]
                });
        }

        // ── Upload lên Cloudinary ──
        var uploadResult = await _imageUploadService.UploadImageToFolderAsync(
            fileStream,
            fileName,
            ThumbnailFolder,
            cancellationToken);

        if (!uploadResult.IsSuccess)
        {
            _logger.LogWarning(
                "Upload blog thumbnail failed. Code: {Code}, Message: {Message}",
                uploadResult.ErrorCode,
                uploadResult.ErrorMessage);

            return Result<UploadBlogThumbnailResponse>.Failure(
                uploadResult.ErrorCode ?? "UPLOAD_ERROR",
                uploadResult.ErrorMessage ?? "Failed to upload thumbnail.");
        }

        _logger.LogInformation("Blog thumbnail uploaded to Cloudinary: {Url}", uploadResult.Data);

        return Result<UploadBlogThumbnailResponse>.Success(
            new UploadBlogThumbnailResponse { Url = uploadResult.Data! });
    }
}
