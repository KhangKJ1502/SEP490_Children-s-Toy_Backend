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
        
        var currentUserId = _currentUser.IsAuthenticated ? _currentUser.AccountId : 0;
        foreach (var dto in dtos)
        {
            var reviewEntity = items.First(r => r.ReviewId == dto.ReviewId);
            dto.LikeCount = reviewEntity.ReviewProductReactions.Count(r => !r.IsDeleted && r.ReactionTypeNavigation.Code.ToLower() == "like");
            dto.IsLiked = currentUserId > 0 && reviewEntity.ReviewProductReactions.Any(r => !r.IsDeleted && r.AccountId == currentUserId && r.ReactionTypeNavigation.Code.ToLower() == "like");
        }

        return Result<PaginatedResponse<ReviewProductListDto>>.Success(
            new PaginatedResponse<ReviewProductListDto>(dtos, count, pageNumber, pageSize));
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

            // 5. AI Moderation: Review đã được tạo với ModerationStatus = "Pending".
            //    AI Sidecar sẽ tự động pick up và xử lý qua polling mỗi 30 giây.
            //    Không cần auto-approve ở đây nữa.

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
            // Dùng GetByIdForUpdateAsync (không filter ModerationStatus) vì review đang ở trạng thái Pending
            var completeReview = await _unitOfWork.Reviews.GetByIdForUpdateAsync(review.ReviewId, cancellationToken);
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

        // Kiểm tra thời hạn 3 ngày từ lúc Approved hoặc Rejected
        var decisionLog = review.ReviewModerationLogs
            .Where(l => l.ImageId == null && 
                       (l.Action == "Approved" || 
                        l.Action == "Rejected" || 
                        (l.Action == "Overridden" && (l.ModerationResult == "Approved" || l.ModerationResult == "Rejected"))))
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefault();

        if (decisionLog == null)
            return Result<ReviewProductDto>.BusinessError("Review is not in a moderated (approved or rejected) state to edit.");

        if ((_timeProvider.UtcNow - decisionLog.CreatedAt).TotalDays > 3)
            return Result<ReviewProductDto>.BusinessError("You can only edit the review within 3 days after it is approved or rejected.");

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

            // AI Moderation: Review đã được reset về ModerationStatus = "Pending" (line 271).
            //    AI Sidecar sẽ tự động pick up và xử lý lại qua polling.

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("User {UserId} edited review {ReviewId}", accountId, reviewId);

            // Dùng GetByIdForUpdateAsync (không filter ModerationStatus) vì review đang ở trạng thái Pending
            var completeReview = await _unitOfWork.Reviews.GetByIdForUpdateAsync(reviewId, cancellationToken);
            return Result<ReviewProductDto>.Success(_mapper.Map<ReviewProductDto>(completeReview ?? trackReview));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update review {ReviewId}", reviewId);
            throw;
        }
    }

    public async Task<Result<PaginatedResponse<UnreviewedProductDto>>> GetUnreviewedProductsAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUser.AccountId;
        pageSize = Math.Min(pageSize, 100);
        pageNumber = Math.Max(pageNumber, 1);

        var items = await _unitOfWork.Reviews.GetUnreviewedProductsAsync(accountId, pageNumber, pageSize, cancellationToken);
        var count = await _unitOfWork.Reviews.GetUnreviewedProductsCountAsync(accountId, cancellationToken);

        var dtos = items.Select(od => new UnreviewedProductDto
        {
            ProductId = od.ProductId,
            ProductName = od.ProductName,
            ProductImage = od.ProductImage,
            OrderId = od.OrderId,
            OrderCode = od.Order.OrderCode,
            CompletedAt = od.Order.CompletedAt,
            RemainingDays = od.Order.CompletedAt.HasValue 
                ? Math.Max(0, 20 - (int)(_timeProvider.UtcNow - od.Order.CompletedAt.Value).TotalDays) 
                : 0
        }).ToList();

        return Result<PaginatedResponse<UnreviewedProductDto>>.Success(
            new PaginatedResponse<UnreviewedProductDto>(dtos, count, pageNumber, pageSize));
    }

    public async Task<Result<PaginatedResponse<MyReviewDto>>> GetMyReviewsAsync(
        MyReviewQueryDto query, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUser.AccountId;
        var pageSize = Math.Min(query.PageSize, 100);
        var pageNumber = Math.Max(query.PageNumber, 1);

        var items = await _unitOfWork.Reviews.GetMyReviewsPagedAsync(
            accountId,
            pageNumber,
            pageSize,
            query.SortBy,
            query.SortDesc,
            query.ModerationStatus,
            cancellationToken);

        var count = await _unitOfWork.Reviews.GetMyReviewsCountAsync(
            accountId,
            query.ModerationStatus,
            cancellationToken);

        var dtos = items.Select(r => new MyReviewDto
        {
            ReviewId = r.ReviewId,
            ProductId = r.ProductId,
            ProductName = r.Product?.ProductName ?? "Unknown Product",
            ProductImage = r.Product?.ProductImage?.ImageUrl,
            OrderId = r.OrderId,
            OrderCode = r.Order?.OrderCode ?? "Unknown Order",
            Rating = r.Rating,
            Comment = r.Comment,
            ModerationStatus = r.ModerationStatus,
            IsEdited = r.IsEdited,
            CreatedAt = r.CreatedAt,
            // Calculate ModeratedAt from logs if needed, or we just map it from something.
            // For now, if it's approved, we can assume it was moderated recently, but let's just use UpdatedAt if it's not Pending
            ModeratedAt = (r.ModerationStatus == "Approved" || r.ModerationStatus == "Rejected") ? (r.UpdatedAt ?? r.CreatedAt) : null,
            Images = r.ReviewProductImages.Select(img => new ReviewImageDto
            {
                ReviewProductImageId = img.ReviewProductImageId,
                ImageUrl = img.ImageUrl
            }).ToList(),
            Replies = r.StaffReviewProductReplies.Select(reply => new StaffReplyDto
            {
                ReplyProductId = reply.ReplyProductId,
                StaffId = reply.StaffId,
                StaffName = reply.Staff?.AccountName ?? "Staff",
                Content = reply.Content,
                CreatedAt = reply.CreatedAt,
                UpdatedAt = reply.UpdatedAt
            }).ToList()
        }).ToList();

        return Result<PaginatedResponse<MyReviewDto>>.Success(
            new PaginatedResponse<MyReviewDto>(dtos, count, pageNumber, pageSize));
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
            if (dto.IsDeleted == true)
            {
                review.IsDeleted = true;
                review.UpdatedAt = now;

                foreach (var img in review.ReviewProductImages)
                {
                    img.IsDeleted = true;
                    img.UpdatedAt = now;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.ModerationStatus))
            {
                if (review.ModerationStatus == "Rejected")
                    return Result<AdminReviewDetailDto>.BusinessError("Cannot change moderation status of a rejected review.");

                if (review.ModerationStatus != "ManualReview" && review.ModerationStatus != "Pending" && review.ModerationStatus != "Approved")
                    return Result<AdminReviewDetailDto>.BusinessError("Can only update status from Pending, ManualReview, or Approved.");

                review.ModerationStatus = dto.ModerationStatus;
                review.UpdatedAt = now;

                // Log cho Review
                await _unitOfWork.Reviews.AddModerationLogAsync(new ReviewModerationLog
                {
                    TargetType = "Text",
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

                if (dto.ModerationStatus == "Rejected")
                {
                    var productName = review.Product?.ProductName ?? "product";
                    var orderSuffix = review.Order != null ? $" from order #{review.Order.OrderCode}" : "";

                    var delivery = new Delivery
                    {
                        AccountId = review.AccountId,
                        RecipientType = "CUSTOMER",
                        Channel = "WEB_BELL",
                        NotificationType = "SYSTEM",
                        Title = "Your review was not approved",
                        Message = string.IsNullOrWhiteSpace(dto.Reason)
                            ? $"Your review for product '{productName}'{orderSuffix} has not been approved due to content guidelines violation."
                            : $"Your review for product '{productName}'{orderSuffix} has not been approved due to content guidelines violation: {dto.Reason}",
                        Payload = System.Text.Json.JsonSerializer.Serialize(new { reviewId = review.ReviewId, reason = dto.Reason }),
                        Status = "Unread",
                        ActionTarget = "/profile/reviews",
                        IdempotencyKey = $"moderation:review:{reviewId}:rejected",
                        CreatedAt = now
                    };
                    _unitOfWork.Deliveries.Add(delivery);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Staff {StaffId} updated review {ReviewId}: ModerationStatus={Status}, IsDeleted={IsDeleted}",
                staffId, reviewId, dto.ModerationStatus, dto.IsDeleted);



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
            return Result<StaffReplyDto>.Forbidden("You can only edit or delete your own replies.");

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

    public async Task<Result<ReviewLikeResponseDto>> ToggleLikeAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        var likeType = await _unitOfWork.Reviews.GetReactionTypeByCodeAsync("like", cancellationToken);
        if (likeType == null)
        {
            return Result<ReviewLikeResponseDto>.Failure("REACTION_TYPE_NOT_FOUND", "Reaction type 'like' is not configured.");
        }

        var review = await _unitOfWork.Reviews.GetByIdPublicAsync(reviewId, cancellationToken);
        if (review == null)
        {
            return Result<ReviewLikeResponseDto>.NotFound("Review", reviewId);
        }

        var accountId = _currentUser.AccountId;
        var existing = await _unitOfWork.Reviews.GetReactionAsync(reviewId, accountId, cancellationToken);
        var now = _timeProvider.UtcNow;
        bool isLikedNow = false;

        if (existing != null)
        {
            // Toggle IsDeleted
            existing.IsDeleted = !existing.IsDeleted;
            existing.UpdatedAt = now;
            existing.ReactionTypeId = likeType.ReactionTypeId;
            isLikedNow = !existing.IsDeleted;
        }
        else
        {
            var reaction = new ReviewProductReaction
            {
                ReviewProductId = reviewId,
                AccountId = accountId,
                ReactionTypeId = likeType.ReactionTypeId,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _unitOfWork.Reviews.AddReactionAsync(reaction, cancellationToken);
            isLikedNow = true;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        var likeCount = await _unitOfWork.Reviews.GetLikeCountAsync(reviewId, cancellationToken);

        _logger.LogInformation("User {UserId} toggled like on review {ReviewId}. New state: {IsLiked}, New count: {LikeCount}", accountId, reviewId, isLikedNow, likeCount);

        return Result<ReviewLikeResponseDto>.Success(new ReviewLikeResponseDto
        {
            ReviewId = reviewId,
            LikeCount = likeCount,
            IsLiked = isLikedNow
        });
    }



    // --- Private Helpers ---

    // AutoApproveAsync đã được xóa.
    // Review được tạo với ModerationStatus = "Pending" và AI Sidecar
    // (chạy trên port 8001) sẽ tự động poll DB mỗi 30 giây để kiểm duyệt.
}
