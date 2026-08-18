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

/// <summary>
/// Service triển khai toàn bộ logic nghiệp vụ của chức năng Đánh giá sản phẩm (Product Review).
/// Bao gồm: lấy danh sách đánh giá công khai, tạo đánh giá mới (kèm upload ảnh Cloudinary và kích hoạt AI Moderation),
/// sửa đánh giá (giới hạn 1 lần trong 3 ngày), tra cứu sản phẩm chưa đánh giá, danh sách đánh giá của tôi,
/// quản trị kiểm duyệt đánh giá Admin/Staff, phản hồi của nhân viên và tính năng Like/Unlike.
/// </summary>
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
    private readonly IProductReviewModerationGateway _productReviewModerationGateway;

    /// <summary>
    /// Khởi tạo ReviewService với đầy đủ các dependency cần thiết.
    /// </summary>
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
        ITimeProvider timeProvider,
        IProductReviewModerationGateway productReviewModerationGateway)
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
        _productReviewModerationGateway = productReviewModerationGateway;
    }

    // ==========================================
    // Public / Customer Endpoints
    // ==========================================

    /// <summary>
    /// Lấy danh sách đánh giá công khai đã được duyệt (APPROVED) của một sản phẩm.
    /// Tính toán tổng số LikeCount và cờ IsLiked dựa trên người dùng hiện tại (nếu đã đăng nhập).
    /// </summary>
    public async Task<Result<PaginatedResponse<ReviewProductListDto>>> GetPublicListAsync(
        ReviewQueryDto query, CancellationToken cancellationToken = default)
    {
        // Chuẩn hóa giới hạn phân trang
        var pageSize = Math.Min(query.PageSize, 100);
        var pageNumber = Math.Max(query.PageNumber, 1);

        // Lấy danh sách đánh giá từ repository
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

        // Đếm tổng số đánh giá thỏa mãn bộ lọc
        var count = await _unitOfWork.Reviews.GetPublicCountAsync(
            query.ProductId,
            query.Rating,
            query.HasImage,
            query.SearchTerm,
            cancellationToken);

        var dtos = _mapper.Map<List<ReviewProductListDto>>(items);
        
        // Tính toán thông tin Like cho từng đánh giá
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

    /// <summary>
    /// Tạo mới một đánh giá sản phẩm:
    /// 1. Kiểm tra tính hợp lệ qua FluentValidation.
    /// 2. Kiểm tra xem khách hàng đã đánh giá sản phẩm trong đơn hàng này chưa.
    /// 3. Xác thực đơn hàng phải ở trạng thái "Completed" và hoàn thành trong vòng 20 ngày gần nhất.
    /// 4. Tạo bản ghi ReviewProduct với trạng thái ban đầu là "Pending".
    /// 5. Upload các hình ảnh đính kèm lên Cloudinary.
    /// 6. Kích hoạt AI Moderation Gateway dạng Fire-and-Forget để kiểm duyệt nội dung tự động.
    /// 7. Phát sự kiện thông báo nếu rating thấp (<= 2 sao).
    /// </summary>
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

        // 3. Khởi tạo Review entity với trạng thái ban đầu là Pending
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
            await _unitOfWork.SaveChangesAsync(cancellationToken); // Lưu để sinh ReviewId

            // 4. Upload Images (nếu có gửi kèm)
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
                        // Nếu upload ảnh lỗi thì rollback transaction
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<ReviewProductDto>.BusinessError($"Failed to upload image: {uploadResult.ErrorMessage}");
                    }
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 5. Commit transaction cơ sở dữ liệu
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Kích hoạt AI Moderation chạy ngầm (Async Fire-and-Forget)
            _ = _productReviewModerationGateway.ModerateReviewAsync(review.ReviewId, CancellationToken.None);

            _logger.LogInformation("User {UserId} created review {ReviewId} for product {ProductId}", accountId, review.ReviewId, dto.ProductId);

            // Phát sự kiện thông báo nếu đánh giá có số sao thấp (<= 2 sao)
            if (review.Rating <= 2)
            {
                await _eventPublisher.PublishAsync("Review", review.ReviewId.ToString(), NotificationEventTypes.ReviewLowRating,
                    new { reviewId = review.ReviewId, rating = review.Rating, productId = dto.ProductId }, cancellationToken);
            }

            // Lấy thực thể đầy đủ vừa tạo để ánh xạ trả về
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

    /// <summary>
    /// Chỉnh sửa đánh giá sản phẩm của khách hàng:
    /// - Kiểm tra quyền sở hữu của khách hàng.
    /// - Kiểm tra điều kiện: chỉ được sửa 1 lần duy nhất (`IsEdited == false`).
    /// - Kiểm tra thời hạn: chỉ được sửa trong vòng 3 ngày kể từ khi có quyết định kiểm duyệt (`Approved` hoặc `Rejected`).
    /// - Đặt lại trạng thái `ModerationStatus = "Pending"`, `IsEdited = true`.
    /// - Xóa mềm ảnh cũ và tải lên ảnh mới nếu có.
    /// - Kích hoạt kiểm duyệt lại nội dung qua AI Moderation Gateway.
    /// </summary>
    public async Task<Result<ReviewProductDto>> UpdateReviewAsync(
        int reviewId, UpdateReviewProductDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<ReviewProductDto>();

        var accountId = _currentUser.AccountId;

        // Lấy thông tin đánh giá kèm log kiểm duyệt
        var review = await _unitOfWork.Reviews.GetByIdForAdminAsync(reviewId, cancellationToken);
        if (review == null || review.AccountId != accountId)
            return Result<ReviewProductDto>.NotFound("Review", reviewId);

        // Kiểm tra số lần chỉnh sửa
        if (review.IsEdited)
            return Result<ReviewProductDto>.BusinessError("You have already edited this review once.");

        // Kiểm tra thời hạn 3 ngày kể từ lúc có kết quả duyệt Approved hoặc Rejected
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

        // Lấy thực thể có tracking để cập nhật
        var trackReview = await _unitOfWork.Reviews.GetByIdForUpdateAsync(reviewId, cancellationToken);
        if (trackReview == null) return Result<ReviewProductDto>.NotFound("Review", reviewId);

        var now = _timeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Xử lý xóa mềm đánh giá nếu yêu cầu
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

            // Cập nhật rating và bình luận mới
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

            // Upload danh sách ảnh mới (nếu có)
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

            // Commit Transaction và kích hoạt AI Moderation
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _ = _productReviewModerationGateway.ModerateReviewAsync(reviewId, CancellationToken.None);

            _logger.LogInformation("User {UserId} edited review {ReviewId}", accountId, reviewId);

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

    /// <summary>
    /// Lấy danh sách sản phẩm chưa được đánh giá từ các đơn hàng hoàn tất của khách hàng, tính toán số ngày còn lại để đánh giá.
    /// </summary>
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

    /// <summary>
    /// Lấy danh sách đánh giá của khách hàng hiện tại kèm hình ảnh, phản hồi của staff và trạng thái duyệt.
    /// </summary>
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

    // ==========================================
    // Admin / Staff Moderation Endpoints
    // ==========================================

    /// <summary>
    /// Admin/Staff lấy danh sách đánh giá có phân trang và bộ lọc nâng cao.
    /// </summary>
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

    /// <summary>
    /// Admin/Staff xem thông tin chi tiết một đánh giá kèm toàn bộ hình ảnh, phản hồi staff và log kiểm duyệt.
    /// </summary>
    public async Task<Result<AdminReviewDetailDto>> GetAdminDetailAsync(
        int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetByIdForAdminAsync(reviewId, cancellationToken);
        if (review == null)
            return Result<AdminReviewDetailDto>.NotFound("Review", reviewId);

        var dto = _mapper.Map<AdminReviewDetailDto>(review);
        return Result<AdminReviewDetailDto>.Success(dto);
    }

    /// <summary>
    /// Admin/Staff cập nhật trạng thái kiểm duyệt thủ công (Approved, Rejected, Hidden):
    /// - Kiểm tra trạng thái hiện tại (không cho phép đổi từ Rejected sang trạng thái khác).
    /// - Ghi nhận nhật ký ReviewModerationLog cho Review và toàn bộ ảnh đính kèm.
    /// - Nếu chuyển sang "Rejected", tự động gửi thông báo hệ thống (Delivery) tới tài khoản khách hàng kèm lý do từ chối.
    /// </summary>
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
                if (string.Equals(review.ModerationStatus, dto.ModerationStatus, StringComparison.OrdinalIgnoreCase))
                    return Result<AdminReviewDetailDto>.BusinessError($"Status is already {dto.ModerationStatus}.");

                if (review.ModerationStatus == "Rejected")
                    return Result<AdminReviewDetailDto>.BusinessError("Cannot change moderation status of a rejected review.");

                if (review.ModerationStatus != "ManualReview" && review.ModerationStatus != "Pending" && review.ModerationStatus != "Approved")
                    return Result<AdminReviewDetailDto>.BusinessError("Can only update status from Pending, ManualReview, or Approved.");

                review.ModerationStatus = dto.ModerationStatus;
                review.UpdatedAt = now;

                // Ghi nhận log kiểm duyệt cho nội dung văn bản
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

                // Tự động đồng bộ trạng thái kiểm duyệt cho các ảnh đính kèm chưa xóa
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

                // Gửi thông báo nếu từ chối đánh giá
                if (dto.ModerationStatus == "Rejected")
                {
                    var productName = review.Product?.ProductName ?? "product";
                    var orderSuffix = review.Order != null ? $" from order #{review.Order.OrderCode}" : "";

                    var rawMessage = string.IsNullOrWhiteSpace(dto.Reason)
                        ? $"Your review for product '{productName}'{orderSuffix} has not been approved due to content guidelines violation."
                        : $"Your review for product '{productName}'{orderSuffix} has not been approved due to content guidelines violation: {dto.Reason}";

                    var message = rawMessage;
                    if (message.Length > 2000)
                    {
                        message = message.Substring(0, 1997) + "...";
                    }

                    var delivery = new Delivery
                    {
                        AccountId = review.AccountId,
                        RecipientType = "CUSTOMER",
                        Channel = "WEB_BELL",
                        NotificationType = "SYSTEM",
                        Title = "Your review was not approved",
                        Message = message,
                        Payload = System.Text.Json.JsonSerializer.Serialize(new { reviewId = review.ReviewId, reason = dto.Reason }),
                        Status = "Unread",
                        ActionTarget = "/profile/reviews",
                        IdempotencyKey = $"moderation:review:{reviewId}:rejected",
                        CreatedAt = now
                    };
                    _unitOfWork.Deliveries.Add(delivery);
                }
            }

            _unitOfWork.Reviews.Update(review);
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

    /// <summary>
    /// Tạo phản hồi của nhân viên cho đánh giá sản phẩm và phát sự kiện thông báo cho khách hàng.
    /// </summary>
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

        // Thông báo cho khách hàng rằng nhân viên đã phản hồi đánh giá của họ
        await _eventPublisher.PublishAsync("Review", reviewId.ToString(), NotificationEventTypes.ReviewStaffReplied,
            new { reviewId, accountId = review.AccountId, productId = review.ProductId }, cancellationToken);

        var savedReply = await _unitOfWork.Reviews.GetReplyByIdAsync(reply.ReplyProductId, cancellationToken);
        var staffDto = _mapper.Map<StaffReplyDto>(savedReply);
        
        if (savedReply?.Staff != null)
        {
            staffDto.StaffName = savedReply.Staff.AccountName;
        }
        else
        {
            var staffInfo = await _unitOfWork.Accounts.GetByIdAsync(staffId, cancellationToken);
            if (staffInfo != null) staffDto.StaffName = staffInfo.AccountName;
        }

        return Result<StaffReplyDto>.Success(staffDto);
    }

    /// <summary>
    /// Chỉnh sửa hoặc xóa mềm phản hồi của nhân viên (chỉ nhân viên tạo hoặc Admin mới có quyền sửa).
    /// </summary>
    public async Task<Result<StaffReplyDto>> UpdateReplyAsync(
        int reviewId, int replyId, UpdateStaffReplyDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _updateReplyValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<StaffReplyDto>();

        var reply = await _unitOfWork.Reviews.GetReplyByIdAsync(replyId, cancellationToken);
        if (reply == null || reply.ReviewProductId != reviewId)
            return Result<StaffReplyDto>.NotFound("Reply", replyId);

        // Chỉ nhân viên tạo phản hồi mới được sửa/xóa (hoặc tài khoản Admin)
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

        if (reply.Staff == null)
        {
            var staff = await _unitOfWork.Accounts.GetByIdAsync(reply.StaffId, cancellationToken);
            if (staff != null) reply.Staff = staff;
        }

        return Result<StaffReplyDto>.Success(_mapper.Map<StaffReplyDto>(reply));
    }

    /// <summary>
    /// Khách hàng Toggle Like hoặc Bỏ Like cho một đánh giá sản phẩm.
    /// </summary>
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
            // Toggle cờ IsDeleted
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
}
