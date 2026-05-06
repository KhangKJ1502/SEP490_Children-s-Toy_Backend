using AutoMapper;
using Microsoft.Extensions.Logging;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class BlogService : IBlogService
{
    private const string DraftStatus = "Draft";
    private const string PendingStatus = "Pending";
    private const string ApprovedStatus = "Approved";
    private const string ScheduledStatus = "Scheduled";
    private const string PublishedStatus = "Published";
    private const string RejectedStatus = "Rejected";
    private const string HiddenStatus = "Hidden";

    private static readonly HashSet<string> AllowedSubmitStatus = new(StringComparer.OrdinalIgnoreCase)
    {
        PendingStatus
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<BlogService> _logger;

    public BlogService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<BlogService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
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

        return Result<BlogDetailDto>.Success(_mapper.Map<BlogDetailDto>(blog));
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
            BlogAt = dto.BlogAt,
            Status = DraftStatus,
            Reason = null,
            ApprovedBy = null,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
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

        if (blog.AccountId != _currentUserService.AccountId)
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

        blog.BlogCategoryId = dto.BlogCategoryId;
        blog.BlogTitle = dto.BlogTitle.Trim();
        blog.BlogContent = dto.BlogContent.Trim();
        blog.BlogThumbnail = dto.BlogThumbnail?.Trim();
        blog.BlogAt = dto.BlogAt;
        blog.UpdatedAt = DateTime.UtcNow;

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

        blog.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Blogs.UpdateAsync(blog, cancellationToken);

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
        if (string.Equals(decision, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            blog.Reason = null;
            if (blog.BlogAt.HasValue && blog.BlogAt.Value > DateTime.UtcNow)
            {
                blog.Status = ScheduledStatus;
            }
            else
            {
                blog.Status = PublishedStatus;
                blog.BlogAt ??= DateTime.UtcNow;
                _logger.LogInformation("Blog {BlogId} approved and published immediately (BlogAt <= now).", blogPostId);
            }
        }
        else if (string.Equals(decision, "Rejected", StringComparison.OrdinalIgnoreCase))
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
            return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "Decision must be Approved or Rejected.");
        }

        blog.ApprovedBy = _currentUserService.AccountId;
        blog.UpdatedAt = DateTime.UtcNow;

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
        blog.BlogAt = DateTime.UtcNow;
        blog.Reason = null;
        blog.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Blogs.UpdateAsync(blog, cancellationToken);
        var updated = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        return Result<BlogDetailDto>.Success(_mapper.Map<BlogDetailDto>(updated!));
    }

    public async Task<Result<BlogDetailDto>> UpdateFeaturedAsync(
        int blogPostId,
        UpdateBlogFeaturedDto dto,
        CancellationToken cancellationToken = default)
    {
        _ = blogPostId;
        _ = dto;
        _ = cancellationToken;
        return Result<BlogDetailDto>.BusinessError(
            "Featured status is managed automatically by database triggers based on blog interactions.");
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
        blog.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Blogs.HideAsync(blog, cancellationToken);
        var hiddenBlog = await _unitOfWork.Blogs.GetByIdAsync(blogPostId, cancellationToken);
        return Result<BlogDetailDto>.Success(hiddenBlog != null ? _mapper.Map<BlogDetailDto>(hiddenBlog) : _mapper.Map<BlogDetailDto>(blog));
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

        if (dto.BlogAt.Value <= DateTime.UtcNow)
        {
            return Result<BlogDetailDto>.Failure("VALIDATION_ERROR", "BlogAt must be in the future.");
        }

        blog.BlogAt = dto.BlogAt.Value;
        blog.Status = ScheduledStatus;
        blog.Reason = null;
        blog.UpdatedAt = DateTime.UtcNow;

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
        short blogCategoryId,
        string blogTitle,
        string blogContent,
        CancellationToken cancellationToken)
    {
        if (blogCategoryId <= 0)
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

        var categoryExists = await _unitOfWork.Blogs.BlogCategoryExistsAsync(blogCategoryId, cancellationToken);
        if (!categoryExists)
        {
            return Result<BlogDetailDto>.NotFound("Blog category", blogCategoryId);
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
}
