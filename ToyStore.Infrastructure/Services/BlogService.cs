using AutoMapper;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class BlogService : IBlogService
{
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
    private const string ApprovePublishNowDecision = "ApprovePublishNow";
    private const string ApproveKeepScheduleDecision = "ApproveKeepSchedule";

    private static readonly HashSet<string> AllowedSubmitStatus = new(StringComparer.OrdinalIgnoreCase)
    {
        PendingStatus
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IMapper _mapper;
    private readonly ILogger<BlogService> _logger;
    private readonly ITimeProvider _timeProvider;

    public BlogService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDomainEventPublisher eventPublisher,
        IMapper mapper,
        ILogger<BlogService> logger,
        ITimeProvider timeProvider)
    {
        _unitOfWork         = unitOfWork;
        _currentUserService = currentUserService;
        _eventPublisher     = eventPublisher;
        _mapper             = mapper;
        _logger             = logger;
        _timeProvider       = timeProvider;
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
        return await GetPagedBlogsAsync(pageNumber, pageSize, sortBy, sortDesc, searchTerm, status, featuredOnly, null, false, cancellationToken);
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

        return await GetPagedBlogsAsync(pageNumber, pageSize, sortBy, sortDesc, searchTerm, status, featuredOnly, _currentUserService.AccountId, false, cancellationToken);
    }

    public async Task<Result<PaginatedResponse<BlogListDto>>> SearchPublishedBlogsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedBlogsAsync(pageNumber, pageSize, sortBy, sortDesc, searchTerm, "Published", false, null, true, cancellationToken);
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

    public async Task<Result<BlogDetailDto>> ApproveBlogAsync(int blogPostId, ApproveBlogDto dto, CancellationToken cancellationToken = default)
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

        if (!string.Equals(blog.Status, PendingStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Result<BlogDetailDto>.BusinessError("Only Pending blog can be approved or rejected.");
        }

        var decision = dto.Decision?.Trim() ?? string.Empty;
        var isApprovePublishNow = string.Equals(decision, ApprovePublishNowDecision, StringComparison.OrdinalIgnoreCase);
        var isApproveKeepSchedule = string.Equals(decision, ApproveKeepScheduleDecision, StringComparison.OrdinalIgnoreCase);
        var isApprovedLegacy = string.Equals(decision, "Approved", StringComparison.OrdinalIgnoreCase);
        var isRejected = string.Equals(decision, "Rejected", StringComparison.OrdinalIgnoreCase);

        if (isApprovePublishNow || isApproveKeepSchedule || isApprovedLegacy)
        {
            blog.Reason = null;
            if (isApprovePublishNow || (isApprovedLegacy && dto.PublishNow == true))
            {
                blog.Status = PublishedStatus;
                blog.BlogAt = _timeProvider.UtcNow;
                _logger.LogInformation("Blog {BlogId} approved and published immediately by account {AccountId}.", blogPostId, _currentUserService.AccountId);
            }
            else if (blog.BlogAt.HasValue && blog.BlogAt.Value > _timeProvider.UtcNow)
            {
                blog.Status = ScheduledStatus;
            }
            else
            {
                blog.Status = PublishedStatus;
                blog.BlogAt ??= _timeProvider.UtcNow;
                _logger.LogInformation("Blog {BlogId} approved and published immediately (BlogAt <= now).", blogPostId);
            }
        }
        else if (isRejected)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "Reason is required when rejecting a blog.");
            }
            blog.Status = RejectedStatus;
            blog.Reason = dto.Reason.Trim();
        }
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

    public Task<Result<BlogDetailDto>> UpdateFeaturedAsync(
        int blogPostId,
        UpdateBlogFeaturedDto dto,
        CancellationToken cancellationToken = default)
    {
        _ = blogPostId;
        _ = dto;
        _ = cancellationToken;
        return Task.FromResult(Result<BlogDetailDto>.BusinessError(
            "Featured status is managed automatically by database triggers based on blog interactions."));
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
        var reviewIds = reviews.Select(x => x.ReviewBlogId).ToList();
        var replies = reviewIds.Count == 0
            ? new List<ReviewBlogReply>()
            : await _unitOfWork.Blogs.GetRepliesByReviewIdsAsync(reviewIds, includeHidden, cancellationToken);

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

        var entity = new ReviewBlog
        {
            BlogPostId = blogPostId,
            AccountId = _currentUserService.AccountId,
            Comment = comment,
            IsDeleted = false,
            CreatedAt = _timeProvider.UtcNow
        };

        var created = await _unitOfWork.Blogs.CreateReviewAsync(entity, cancellationToken);
        var loaded = await _unitOfWork.Blogs.GetReviewByIdAsync(created.ReviewBlogId, cancellationToken);
        if (loaded == null)
        {
            return Result<BlogReviewDto>.NotFound("Review", created.ReviewBlogId);
        }

        return Result<BlogReviewDto>.Success(MapReview(loaded));
    }

    public async Task<Result<BlogReviewReplyDto>> CreateBlogReviewReplyAsync(int reviewBlogId, CreateBlogReviewReplyDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.AccountId <= 0)
        {
            return Result<BlogReviewReplyDto>.Unauthorized();
        }

        var review = await _unitOfWork.Blogs.GetReviewByIdAsync(reviewBlogId, cancellationToken);
        if (review == null)
        {
            return Result<BlogReviewReplyDto>.NotFound("Review", reviewBlogId);
        }

        if (review.IsDeleted && !IsPrivilegedUser())
        {
            return Result<BlogReviewReplyDto>.BusinessError("Cannot reply to hidden review.");
        }

        var comment = dto.Comment?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(comment) || comment.Length > MaxReviewCommentLength)
        {
            return Result<BlogReviewReplyDto>.Failure("VALIDATION_ERROR", "Comment is required and must be at most 500 characters.");
        }

        if (dto.ParentReplyId.HasValue)
        {
            var parentReply = await _unitOfWork.Blogs.GetReplyByIdAsync(dto.ParentReplyId.Value, cancellationToken);
            if (parentReply == null || parentReply.ReviewBlogId != reviewBlogId)
            {
                return Result<BlogReviewReplyDto>.Failure("VALIDATION_ERROR", "Parent reply is invalid.");
            }
        }

        var entity = new ReviewBlogReply
        {
            ReviewBlogId = reviewBlogId,
            AccountId = _currentUserService.AccountId,
            ParentReplyId = dto.ParentReplyId,
            ReplyToAccountId = dto.ReplyToAccountId,
            Comment = comment,
            IsDeleted = false,
            CreatedAt = _timeProvider.UtcNow
        };

        var created = await _unitOfWork.Blogs.CreateReplyAsync(entity, cancellationToken);
        var loaded = await _unitOfWork.Blogs.GetReplyByIdAsync(created.ReplyBlogId, cancellationToken);
        if (loaded == null)
        {
            return Result<BlogReviewReplyDto>.NotFound("Reply", created.ReplyBlogId);
        }

        return Result<BlogReviewReplyDto>.Success(MapReply(loaded));
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

    public async Task<Result<PaginatedResponse<BlogReviewDto>>> GetBlogReviewsForManagementAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsPrivilegedUser())
        {
            return Result<PaginatedResponse<BlogReviewDto>>.Unauthorized();
        }

        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<BlogReviewDto>>.Failure("VALIDATION_ERROR", "Invalid pagination values.");
        }

        var reviews = await _unitOfWork.Blogs.GetPagedReviewsForManagementAsync(pageNumber, pageSize, searchTerm, status, cancellationToken);
        var reviewIds = reviews.Select(x => x.ReviewBlogId).ToList();
        var replies = reviewIds.Count == 0
            ? new List<ReviewBlogReply>()
            : await _unitOfWork.Blogs.GetRepliesByReviewIdsAsync(reviewIds, includeHidden: true, cancellationToken);
        var reviewCounts = await _unitOfWork.Blogs.GetReviewReactionCountsByIdsAsync(reviewIds, cancellationToken);
        var replyIds = replies.Select(x => x.ReplyBlogId).ToList();
        var replyCounts = await _unitOfWork.Blogs.GetReplyReactionCountsByIdsAsync(replyIds, cancellationToken);
        var mapped = MapReviewThreads(
            reviews,
            replies,
            includeHidden: true,
            reviewCounts,
            replyCounts,
            new Dictionary<int, string>(),
            new Dictionary<int, string>());
        var totalCount = await _unitOfWork.Blogs.CountReviewsForManagementAsync(searchTerm, status, cancellationToken);
        return Result<PaginatedResponse<BlogReviewDto>>.Success(new PaginatedResponse<BlogReviewDto>(mapped, totalCount, pageNumber, pageSize));
    }

    public async Task<Result<BlogReviewDto>> UpdateBlogReviewStatusAsync(int reviewBlogId, UpdateBlogReviewStatusDto dto, CancellationToken cancellationToken = default)
    {
        if (!IsPrivilegedUser())
        {
            return Result<BlogReviewDto>.Unauthorized();
        }

        var review = await _unitOfWork.Blogs.GetReviewByIdAsync(reviewBlogId, cancellationToken);
        if (review == null)
        {
            return Result<BlogReviewDto>.NotFound("Review", reviewBlogId);
        }

        var isHidden = ParseHiddenStatus(dto.Status);
        if (!isHidden.HasValue)
        {
            return Result<BlogReviewDto>.Failure("VALIDATION_ERROR", "Status must be Visible or Hidden.");
        }

        review.IsDeleted = isHidden.Value;
        review.UpdatedAt = _timeProvider.UtcNow;
        await _unitOfWork.Blogs.UpdateReviewAsync(review, cancellationToken);
        var updated = await _unitOfWork.Blogs.GetReviewByIdAsync(reviewBlogId, cancellationToken);
        return Result<BlogReviewDto>.Success(MapReview(updated!));
    }

    public async Task<Result<BlogReviewReplyDto>> UpdateBlogReplyStatusAsync(int replyBlogId, UpdateBlogReviewStatusDto dto, CancellationToken cancellationToken = default)
    {
        if (!IsPrivilegedUser())
        {
            return Result<BlogReviewReplyDto>.Unauthorized();
        }

        var reply = await _unitOfWork.Blogs.GetReplyByIdAsync(replyBlogId, cancellationToken);
        if (reply == null)
        {
            return Result<BlogReviewReplyDto>.NotFound("Reply", replyBlogId);
        }

        var isHidden = ParseHiddenStatus(dto.Status);
        if (!isHidden.HasValue)
        {
            return Result<BlogReviewReplyDto>.Failure("VALIDATION_ERROR", "Status must be Visible or Hidden.");
        }

        reply.IsDeleted = isHidden.Value;
        reply.UpdatedAt = _timeProvider.UtcNow;
        await _unitOfWork.Blogs.UpdateReplyAsync(reply, cancellationToken);
        var updated = await _unitOfWork.Blogs.GetReplyByIdAsync(replyBlogId, cancellationToken);
        return Result<BlogReviewReplyDto>.Success(MapReply(updated!));
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
        CancellationToken cancellationToken)
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
            cancellationToken);

        var totalCount = await _unitOfWork.Blogs.CountAsync(searchTerm, status, featuredOnly, createdByAccountId, onlyPublished, cancellationToken);
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
        var roleName = _currentUserService.RoleName;
        return string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, "Staff", StringComparison.OrdinalIgnoreCase);
    }

    private static bool? ParseHiddenStatus(string? status)
    {
        if (string.Equals(status?.Trim(), "Visible", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(status?.Trim(), "Hidden", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return null;
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
            AccountImageUrl = review.Account?.ImageUrl,
            Comment = review.Comment ?? string.Empty,
            Status = review.IsDeleted ? "Hidden" : "Visible",
            LikeCount = GetReactionCount(counts, ReactionLike),
            LoveCount = GetReactionCount(counts, ReactionLove),
            HahaCount = GetReactionCount(counts, ReactionHaha),
            CurrentUserReaction = currentUserReaction,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }

    private static BlogReviewReplyDto MapReply(ReviewBlogReply reply, Dictionary<string, int>? counts = null, string? currentUserReaction = null)
    {
        return new BlogReviewReplyDto
        {
            ReplyBlogId = reply.ReplyBlogId,
            ReviewBlogId = reply.ReviewBlogId,
            AccountId = reply.AccountId,
            AccountName = reply.Account?.AccountName ?? string.Empty,
            AccountImageUrl = reply.Account?.ImageUrl,
            ParentReplyId = reply.ParentReplyId,
            ReplyToAccountId = reply.ReplyToAccountId,
            ReplyToAccountName = reply.ReplyToAccount?.AccountName,
            Comment = reply.Comment,
            Status = reply.IsDeleted ? "Hidden" : "Visible",
            LikeCount = GetReactionCount(counts, ReactionLike),
            LoveCount = GetReactionCount(counts, ReactionLove),
            HahaCount = GetReactionCount(counts, ReactionHaha),
            CurrentUserReaction = currentUserReaction,
            CreatedAt = reply.CreatedAt,
            UpdatedAt = reply.UpdatedAt
        };
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
}
