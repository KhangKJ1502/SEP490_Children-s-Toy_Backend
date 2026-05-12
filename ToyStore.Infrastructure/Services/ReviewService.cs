using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Reviews;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IImageUploadService _imageUploadService;
    private readonly ILogger<ReviewService> _logger;

    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IValidator<CreateReviewProductDto> _createValidator;
    private readonly IValidator<UpdateReviewProductDto> _updateValidator;
    private readonly IValidator<UpdateModerationStatusDto> _updateStatusValidator;
    private readonly IValidator<CreateStaffReplyDto> _createReplyValidator;
    private readonly IValidator<UpdateStaffReplyDto> _updateReplyValidator;
    private readonly ITimeProvider _timeProvider;

    public ReviewService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUser,
        IImageUploadService imageUploadService,
        IDomainEventPublisher eventPublisher,
        ILogger<ReviewService> logger,
        IValidator<CreateReviewProductDto> createValidator,
        IValidator<UpdateReviewProductDto> updateValidator,
        IValidator<UpdateModerationStatusDto> updateStatusValidator,
        IValidator<CreateStaffReplyDto> createReplyValidator,
        IValidator<UpdateStaffReplyDto> updateReplyValidator,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _imageUploadService = imageUploadService;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _updateStatusValidator = updateStatusValidator;
        _createReplyValidator = createReplyValidator;
        _updateReplyValidator = updateReplyValidator;
        _timeProvider = timeProvider;
    }

    // --- Public / Customer ---

    public async Task<Result<PaginatedResponse<ReviewProductListDto>>> GetPublicListAsync(
        ReviewQueryDto query, CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Min(query.PageSize, 100);
        var pageNumber = Math.Max(query.PageNumber, 1);

        var items = await _unitOfWork.Reviews.GetPublicPagedAsync(
            query.ProductId,
            pageNumber,
            pageSize,
            query.SortBy,
            query.SortDesc,
            query.Rating,
            query.HasImage,
            query.SearchTerm,
            cancellationToken);

        var count = await _unitOfWork.Reviews.GetPublicCountAsync(
            query.ProductId,
            query.Rating,
            query.HasImage,
            query.SearchTerm,
            cancellationToken);

        var dtos = _mapper.Map<List<ReviewProductListDto>>(items);
        return Result<PaginatedResponse<ReviewProductListDto>>.Success(
            new PaginatedResponse<ReviewProductListDto>(dtos, count, pageNumber, pageSize));
    }

    public async Task<Result<ReviewProductDto>> GetPublicDetailAsync(
        int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetByIdPublicAsync(reviewId, cancellationToken);
        if (review == null)
            return Result<ReviewProductDto>.NotFound("Review", reviewId);

        var dto = _mapper.Map<ReviewProductDto>(review);
        return Result<ReviewProductDto>.Success(dto);
    }

    public async Task<Result<ReviewProductDto>> CreateReviewAsync(
        CreateReviewProductDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<ReviewProductDto>();

        var accountId = _currentUser.AccountId;

        // 1. Kiểm tra tồn tại Review (chỉ 1 review cho 1 product trong 1 order)
        var exists = await _unitOfWork.Reviews.ExistsByAccountOrderProductAsync(accountId, dto.OrderId, dto.ProductId, cancellationToken);
        if (exists)
            return Result<ReviewProductDto>.Conflict("You have already reviewed this product for this order.");

        // 2. Kiểm tra Order hợp lệ
        var order = await _unitOfWork.Reviews.GetOrderForReviewAsync(dto.OrderId, accountId, cancellationToken);
        if (order == null)
            return Result<ReviewProductDto>.NotFound("Order", dto.OrderId);

        if (order.Status.StatusName != "Completed")
            return Result<ReviewProductDto>.BusinessError("You can only review products from completed orders.");

        if (order.CompletedAt == null || (_timeProvider.UtcNow - order.CompletedAt.Value).TotalDays > 20)
            return Result<ReviewProductDto>.BusinessError("You can only review within 20 days after the order is completed.");

        if (!order.OrderDetails.Any(od => od.ProductId == dto.ProductId))
            return Result<ReviewProductDto>.BusinessError("The product is not part of this order.");

        var now = _timeProvider.UtcNow;

        // 3. Khởi tạo Review entity
        var review = new ReviewProduct
        {
            AccountId = accountId,
            ProductId = dto.ProductId,
            OrderId = dto.OrderId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            ModerationStatus = "Pending",
            IsDeleted = false,
            IsEdited = false,
            CreatedAt = now
        };

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Reviews.AddReviewAsync(review, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken); // Save to generate ReviewId

            // 4. Upload Images (nếu có)
            var uploadedImages = new List<ReviewProductImage>();
            if (dto.Images != null && dto.Images.Any())
            {
                foreach (var file in dto.Images)
                {
                    using var stream = file.OpenReadStream();
                    var uploadResult = await _imageUploadService.UploadImageToFolderAsync(
                        stream, file.FileName, "reviews", cancellationToken);

                    if (uploadResult.IsSuccess)
                    {
                        var image = new ReviewProductImage
                        {
                            ReviewProductId = review.ReviewId,
                            ImageUrl = uploadResult.Data!,
                            ModerationStatus = "Pending",
                            IsDeleted = false,
                            CreatedAt = now
                        };
                        await _unitOfWork.Reviews.AddImageAsync(image, cancellationToken);
                        uploadedImages.Add(image);
                    }
                    else
                    {
                        // Nếu upload ảnh lỗi thì rollback tất cả
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<ReviewProductDto>.BusinessError($"Failed to upload image: {uploadResult.ErrorMessage}");
                    }
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 5. Auto-Approve Flow
            await AutoApproveAsync(review, uploadedImages, now, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("User {UserId} created review {ReviewId} for product {ProductId}", accountId, review.ReviewId, dto.ProductId);

            // Publish notification events (fire-and-forget)
            if (review.Rating <= 2)
            {
                await _eventPublisher.PublishAsync("Review", review.ReviewId.ToString(), NotificationEventTypes.ReviewLowRating,
                    new { reviewId = review.ReviewId, rating = review.Rating, productId = dto.ProductId }, cancellationToken);

            }

            // Fetch the fully populated review to map and return
            var completeReview = await _unitOfWork.Reviews.GetByIdPublicAsync(review.ReviewId, cancellationToken);
            return Result<ReviewProductDto>.Success(_mapper.Map<ReviewProductDto>(completeReview ?? review));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create review for Order {OrderId}, Product {ProductId}", dto.OrderId, dto.ProductId);
            throw;
        }
    }

    public async Task<Result<ReviewProductDto>> UpdateReviewAsync(
        int reviewId, UpdateReviewProductDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<ReviewProductDto>();

        var accountId = _currentUser.AccountId;

        var review = await _unitOfWork.Reviews.GetByIdForAdminAsync(reviewId, cancellationToken); // Dùng admin get để lấy log
        if (review == null || review.AccountId != accountId)
            return Result<ReviewProductDto>.NotFound("Review", reviewId);

        if (review.IsEdited)
            return Result<ReviewProductDto>.BusinessError("You have already edited this review once.");

        // Kiểm tra thời hạn 3 ngày từ lúc Approved
        var approvedLog = review.ReviewModerationLogs
            .Where(l => l.Action == "Approved" && l.ImageId == null)
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefault();

        if (approvedLog == null)
            return Result<ReviewProductDto>.BusinessError("Review is not in an approved state to edit.");

        if ((_timeProvider.UtcNow - approvedLog.CreatedAt).TotalDays > 3)
            return Result<ReviewProductDto>.BusinessError("You can only edit the review within 3 days after it is approved.");

        // Lấy entity track để update
        var trackReview = await _unitOfWork.Reviews.GetByIdForUpdateAsync(reviewId, cancellationToken);
        if (trackReview == null) return Result<ReviewProductDto>.NotFound("Review", reviewId);

        var now = _timeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (dto.IsDeleted == true)
            {
                trackReview.IsDeleted = true;
                trackReview.UpdatedAt = now;

                foreach (var img in trackReview.ReviewProductImages)
                {
                    img.IsDeleted = true;
                    img.UpdatedAt = now;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("User {UserId} soft-deleted review {ReviewId}", accountId, reviewId);
                return Result<ReviewProductDto>.Success(_mapper.Map<ReviewProductDto>(trackReview));
            }

            if (dto.Rating.HasValue) trackReview.Rating = dto.Rating.Value;
            if (dto.Comment != null) trackReview.Comment = dto.Comment;

            trackReview.IsEdited = true;
            trackReview.ModerationStatus = "Pending";
            trackReview.UpdatedAt = now;

            // Xử lý ảnh: Soft delete tất cả ảnh cũ
            foreach (var img in trackReview.ReviewProductImages)
            {
                img.IsDeleted = true;
                img.UpdatedAt = now;
            }

            // Upload ảnh mới
            var uploadedImages = new List<ReviewProductImage>();
            if (dto.Images != null && dto.Images.Any())
            {
                foreach (var file in dto.Images)
                {
                    using var stream = file.OpenReadStream();
                    var uploadResult = await _imageUploadService.UploadImageToFolderAsync(
                        stream, file.FileName, "reviews", cancellationToken);

                    if (uploadResult.IsSuccess)
                    {
                        var image = new ReviewProductImage
                        {
                            ReviewProductId = trackReview.ReviewId,
                            ImageUrl = uploadResult.Data!,
                            ModerationStatus = "Pending",
                            IsDeleted = false,
                            CreatedAt = now
                        };
                        await _unitOfWork.Reviews.AddImageAsync(image, cancellationToken);
                        uploadedImages.Add(image);
                    }
                    else
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<ReviewProductDto>.BusinessError($"Failed to upload image: {uploadResult.ErrorMessage}");
                    }
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Chạy lại Auto-Approve Flow
            await AutoApproveAsync(trackReview, uploadedImages, now, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("User {UserId} edited review {ReviewId}", accountId, reviewId);

            var completeReview = await _unitOfWork.Reviews.GetByIdPublicAsync(reviewId, cancellationToken);
            return Result<ReviewProductDto>.Success(_mapper.Map<ReviewProductDto>(completeReview ?? trackReview));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update review {ReviewId}", reviewId);
            throw;
        }
    }



    // --- Admin / Staff ---

    public async Task<Result<PaginatedResponse<AdminReviewListDto>>> GetAdminListAsync(
        AdminReviewQueryDto query, CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Min(query.PageSize, 100);
        var pageNumber = Math.Max(query.PageNumber, 1);

        var items = await _unitOfWork.Reviews.GetAdminPagedAsync(
            pageNumber,
            pageSize,
            query.SortBy,
            query.SortDesc,
            query.ModerationStatus,
            query.ProductId,
            query.AccountId,
            query.OrderId,
            query.SearchTerm,
            query.FromDate,
            query.ToDate,
            query.IsDeleted,
            cancellationToken);

        var count = await _unitOfWork.Reviews.GetAdminCountAsync(
            query.ModerationStatus,
            query.ProductId,
            query.AccountId,
            query.OrderId,
            query.SearchTerm,
            query.FromDate,
            query.ToDate,
            query.IsDeleted,
            cancellationToken);

        var dtos = _mapper.Map<List<AdminReviewListDto>>(items);
        return Result<PaginatedResponse<AdminReviewListDto>>.Success(
            new PaginatedResponse<AdminReviewListDto>(dtos, count, pageNumber, pageSize));
    }

    public async Task<Result<AdminReviewDetailDto>> GetAdminDetailAsync(
        int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetByIdForAdminAsync(reviewId, cancellationToken);
        if (review == null)
            return Result<AdminReviewDetailDto>.NotFound("Review", reviewId);

        var dto = _mapper.Map<AdminReviewDetailDto>(review);
        return Result<AdminReviewDetailDto>.Success(dto);
    }

    public async Task<Result<AdminReviewDetailDto>> UpdateModerationStatusAsync(
        int reviewId, UpdateModerationStatusDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _updateStatusValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<AdminReviewDetailDto>();

        var review = await _unitOfWork.Reviews.GetByIdForUpdateAsync(reviewId, cancellationToken);
        if (review == null)
            return Result<AdminReviewDetailDto>.NotFound("Review", reviewId);

        var now = _timeProvider.UtcNow;
        var staffId = _currentUser.AccountId;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            review.ModerationStatus = dto.ModerationStatus;
            review.UpdatedAt = now;

            // Log cho Review
            await _unitOfWork.Reviews.AddModerationLogAsync(new ReviewModerationLog
            {
                TargetType = "Review",
                ReviewId = reviewId,
                ModeratorType = "Staff",
                ModeratedBy = staffId,
                Action = "Overridden",
                ModerationResult = dto.ModerationStatus,
                Reason = dto.Reason ?? $"Manually overridden to {dto.ModerationStatus}",
                CreatedAt = now
            }, cancellationToken);

            // Tự động override status của các ảnh chưa xoá theo review
            foreach (var img in review.ReviewProductImages.Where(i => !i.IsDeleted))
            {
                img.ModerationStatus = dto.ModerationStatus;
                img.UpdatedAt = now;

                await _unitOfWork.Reviews.AddModerationLogAsync(new ReviewModerationLog
                {
                    TargetType = "Image",
                    ReviewId = reviewId,
                    ImageId = img.ReviewProductImageId,
                    ModeratorType = "Staff",
                    ModeratedBy = staffId,
                    Action = "Overridden",
                    ModerationResult = dto.ModerationStatus,
                    Reason = $"Cascade from review override to {dto.ModerationStatus}",
                    CreatedAt = now
                }, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Staff {StaffId} overridden moderation status for review {ReviewId} to {Status}", staffId, reviewId, dto.ModerationStatus);

            // Publish when set to ManualReview
            if (dto.ModerationStatus == "ManualReview")
            {
                await _eventPublisher.PublishAsync("Review", reviewId.ToString(), NotificationEventTypes.ReviewNeedsModeration,
                    new { reviewId, moderatedBy = staffId }, cancellationToken);
            }

            var completeReview = await _unitOfWork.Reviews.GetByIdForAdminAsync(reviewId, cancellationToken);
            return Result<AdminReviewDetailDto>.Success(_mapper.Map<AdminReviewDetailDto>(completeReview));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update moderation status for review {ReviewId}", reviewId);
            throw;
        }
    }

    public async Task<Result<StaffReplyDto>> CreateReplyAsync(
        int reviewId, CreateStaffReplyDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _createReplyValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<StaffReplyDto>();

        var review = await _unitOfWork.Reviews.GetByIdForUpdateAsync(reviewId, cancellationToken);
        if (review == null)
            return Result<StaffReplyDto>.NotFound("Review", reviewId);

        var staffId = _currentUser.AccountId;
        var now = _timeProvider.UtcNow;

        var reply = new StaffReviewProductReply
        {
            ReviewProductId = reviewId,
            StaffId = staffId,
            Content = dto.Content,
            IsDeleted = false,
            CreatedAt = now
        };

        await _unitOfWork.Reviews.AddReplyAsync(reply, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Staff {StaffId} replied to review {ReviewId}", staffId, reviewId);

        // Notify customer that staff replied to their review
        await _eventPublisher.PublishAsync("Review", reviewId.ToString(), NotificationEventTypes.ReviewStaffReplied,
            new { reviewId, accountId = review.AccountId, productId = review.ProductId }, cancellationToken);

        var savedReply = await _unitOfWork.Reviews.GetReplyByIdAsync(reply.ReplyProductId, cancellationToken);
        var staffDto = _mapper.Map<StaffReplyDto>(savedReply);
        // Explicitly set StaffName since it might not be eagerly loaded if we just created it without tracking include
        if (savedReply?.Staff != null)
        {
            staffDto.StaffName = savedReply.Staff.AccountName;
        }
        else
        {
            // fallback, get staff info
            var staffInfo = await _unitOfWork.Accounts.GetByIdAsync(staffId, cancellationToken);
            if (staffInfo != null) staffDto.StaffName = staffInfo.AccountName;
        }

        return Result<StaffReplyDto>.Success(staffDto);
    }

    public async Task<Result<StaffReplyDto>> UpdateReplyAsync(
        int reviewId, int replyId, UpdateStaffReplyDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _updateReplyValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<StaffReplyDto>();

        var reply = await _unitOfWork.Reviews.GetReplyByIdAsync(replyId, cancellationToken);
        if (reply == null || reply.ReviewProductId != reviewId)
            return Result<StaffReplyDto>.NotFound("Reply", replyId);

        // Chỉ staff tạo reply mới được sửa/xoá (hoặc admin)
        if (reply.StaffId != _currentUser.AccountId && _currentUser.RoleName != "Admin")
            return Result<StaffReplyDto>.Failure("UNAUTHORIZED", "You can only edit or delete your own replies.");

        if (dto.IsDeleted == true)
        {
            reply.IsDeleted = true;
            reply.UpdatedAt = _timeProvider.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Staff {StaffId} soft-deleted reply {ReplyId} for review {ReviewId}", _currentUser.AccountId, replyId, reviewId);
        }
        else
        {
            if (dto.Content != null)
            {
                reply.Content = dto.Content;
            }

            reply.UpdatedAt = _timeProvider.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Staff {StaffId} updated reply {ReplyId} for review {ReviewId}", _currentUser.AccountId, replyId, reviewId);
        }

        // Load account to map StaffName
        if (reply.Staff == null)
        {
            var staff = await _unitOfWork.Accounts.GetByIdAsync(reply.StaffId, cancellationToken);
            if (staff != null) reply.Staff = staff;
        }

        return Result<StaffReplyDto>.Success(_mapper.Map<StaffReplyDto>(reply));
    }



    // --- Private Helpers ---

    private async Task AutoApproveAsync(
        ReviewProduct review,
        List<ReviewProductImage> images,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // PHASE HIỆN TẠI (chưa tích hợp AI): Tự động approve
        review.ModerationStatus = "Approved";
        review.UpdatedAt = now;

        await _unitOfWork.Reviews.AddModerationLogAsync(new ReviewModerationLog
        {
            TargetType = "Review",
            ReviewId = review.ReviewId,
            ModeratorType = "AI",
            Action = "Approved",
            AiModelVersion = "auto-approve-v0",
            ModerationResult = "Approved",
            Reason = "AI moderation not yet integrated — auto approved",
            CreatedAt = now
        }, cancellationToken);

        foreach (var img in images)
        {
            img.ModerationStatus = "Approved";
            img.UpdatedAt = now;

            await _unitOfWork.Reviews.AddModerationLogAsync(new ReviewModerationLog
            {
                TargetType = "Image",
                ReviewId = review.ReviewId,
                ImageId = img.ReviewProductImageId,
                ModeratorType = "AI",
                Action = "Approved",
                AiModelVersion = "auto-approve-v0",
                ModerationResult = "Approved",
                Reason = "AI moderation not yet integrated — auto approved",
                CreatedAt = now
            }, cancellationToken);
        }
    }
}
