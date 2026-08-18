using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Promotions;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Service xử lý toàn bộ nghiệp vụ quản lý Chương trình Khuyến mãi (Promotion), Flash Sale theo khung giờ và giảm giá trực tiếp theo sản phẩm.
/// </summary>
public class PromotionService : IPromotionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PromotionService> _logger;
    private readonly ICartService _cartService;
    private readonly IValidator<CreatePromotionDto> _createValidator;
    private readonly IValidator<UpdatePromotionDto> _updateValidator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITimeProvider _timeProvider;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Khởi tạo PromotionService và tiêm các phụ thuộc cần thiết.
    /// </summary>
    /// <param name="unitOfWork">Unit of Work quản lý các Repository và Database Transaction.</param>
    /// <param name="mapper">AutoMapper chuyển đổi dữ liệu Entity sang DTO và ngược lại.</param>
    /// <param name="logger">Logger ghi vết lỗi và thông tin hoạt động.</param>
    /// <param name="cartService">Service giỏ hàng dùng để thông báo cập nhật lại giá khi khuyến mãi thay đổi.</param>
    /// <param name="createValidator">FluentValidation kiểm tra dữ liệu tạo mới chương trình khuyến mãi.</param>
    /// <param name="updateValidator">FluentValidation kiểm tra dữ liệu cập nhật chương trình khuyến mãi.</param>
    /// <param name="currentUserService">Service cung cấp thông tin tài khoản người dùng đang đăng nhập.</param>
    /// <param name="timeProvider">Service cung cấp thời gian hệ thống chuẩn hóa UTC.</param>
    /// <param name="configuration">Cấu hình hệ thống (đọc số ngày hiển thị Flash Sale).</param>
    public PromotionService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PromotionService> logger,
        ICartService cartService,
        IValidator<CreatePromotionDto> createValidator,
        IValidator<UpdatePromotionDto> updateValidator,
        ICurrentUserService currentUserService,
        ITimeProvider timeProvider,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _cartService = cartService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _configuration = configuration;
    }

    /// <summary>
    /// Lấy danh sách chương trình khuyến mãi có phân trang, hỗ trợ tìm kiếm theo từ khóa và lọc theo trạng thái.
    /// </summary>
    /// <param name="pageNumber">Số thứ tự trang (1-based).</param>
    /// <param name="pageSize">Số bản ghi trên mỗi trang (1 - 100).</param>
    /// <param name="sortBy">Trường cần sắp xếp.</param>
    /// <param name="sortDesc">true để giảm dần, false để tăng dần.</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm theo tên hoặc mô tả.</param>
    /// <param name="status">Trạng thái lọc (Scheduled, Active, Inactive, Expired).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa PaginatedResponse danh sách PromotionListDto.</returns>
    public async Task<Result<PaginatedResponse<PromotionListDto>>> GetPromotionsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra tính hợp lệ của tham số phân trang
        if (pageNumber < 1)
        {
            return Result<PaginatedResponse<PromotionListDto>>.Failure(
                "VALIDATION_ERROR", "Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<PromotionListDto>>.Failure(
                "VALIDATION_ERROR", "Page size must be between 1 and 100.");
        }

        // 2. Truy vấn dữ liệu phân trang từ Repository
        var pagedPromotions = await _unitOfWork.Promotions.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            status,
            cancellationToken);

        // 3. Ánh xạ danh sách Entity sang DTO hiển thị rút gọn
        var items = _mapper.Map<List<PromotionListDto>>(pagedPromotions.Items);

        _logger.LogInformation(
            "Retrieved promotions list with PageNumber {PageNumber}, PageSize {PageSize}, SearchTerm {SearchTerm}, Status {Status}",
            pageNumber,
            pageSize,
            searchTerm,
            status);

        var response = new PaginatedResponse<PromotionListDto>(
            items,
            pagedPromotions.TotalCount,
            pageNumber,
            pageSize);

        return Result<PaginatedResponse<PromotionListDto>>.Success(response);
    }

    /// <summary>
    /// Lấy danh sách các chương trình FLASH_SALE đang diễn ra hoặc sắp diễn ra để hiển thị công khai ở trang chủ.
    /// </summary>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách PromotionDto chứa thông tin Flash Sale, khung giờ và sản phẩm.</returns>
    public async Task<Result<List<PromotionDto>>> GetFlashSalePromotionsAsync(
        CancellationToken cancellationToken = default)
    {
        // Đọc số ngày hiển thị Flash Sale trước từ cấu hình (mặc định 2 ngày)
        int visibilityDays = _configuration.GetValue<int>("Promotions:FlashSaleVisibilityDays", 2);
        var promotions = await _unitOfWork.Promotions.GetFlashSalePromotionsAsync(visibilityDays, cancellationToken);
        var dtos = _mapper.Map<List<PromotionDto>>(promotions);

        _logger.LogInformation(
            "Retrieved {Count} FLASH_SALE promotions for public display",
            dtos.Count);

        return Result<List<PromotionDto>>.Success(dtos);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một chương trình khuyến mãi theo ID (nạp kèm danh sách sản phẩm hoặc khung giờ Flash Sale).
    /// </summary>
    /// <param name="promotionId">Mã ID chương trình khuyến mãi.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa PromotionDto chi tiết hoặc 404 NotFound nếu không tìm thấy.</returns>
    public async Task<Result<PromotionDto>> GetPromotionByIdAsync(
        int promotionId,
        CancellationToken cancellationToken = default)
    {
        if (promotionId <= 0)
        {
            return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Promotion ID must be greater than 0.");
        }

        // Nạp kèm đầy đủ cây quan hệ thực thể
        var promotion = await _unitOfWork.Promotions.GetByIdAsync(
            promotionId,
            cancellationToken,
            includeProperties: "ProductPromotions,ProductPromotions.Product,PromotionTimeSlots,PromotionTimeSlots.PromotionProductSlots,PromotionTimeSlots.PromotionProductSlots.Product"
        );

        if (promotion is null)
        {
            return Result<PromotionDto>.NotFound("Promotion", promotionId);
        }

        return Result<PromotionDto>.Success(_mapper.Map<PromotionDto>(promotion));
    }

    /// <summary>
    /// Tạo mới một chương trình khuyến mãi (NORMAL/DISCOUNT hoặc FLASH_SALE) trong Database Transaction an toàn.
    /// </summary>
    /// <param name="request">DTO chứa thông tin tạo mới chương trình khuyến mãi.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa thông tin PromotionDto vừa tạo.</returns>
    public async Task<Result<PromotionDto>> CreatePromotionAsync(
        CreatePromotionDto request,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra hợp lệ dữ liệu đầu vào qua FluentValidation
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return validationResult.ToResult<PromotionDto>();
        }

        // 2. Kiểm tra trùng lặp tên chương trình khuyến mãi
        var promotionNameExists = await _unitOfWork.Promotions.ExistsPromotionNameAsync(
            request.PromotionName,
            null,
            cancellationToken);

        if (promotionNameExists)
        {
            return Result<PromotionDto>.Conflict("Promotion name already exists.");
        }

        // 3. Ánh xạ sang Entity và gán các trường quản lý hệ thống
        var promotion = _mapper.Map<Promotion>(request);
        promotion.CreatedBy = _currentUserService.AccountId;
        promotion.CreatedAt = _timeProvider.UtcNow;
        promotion.UpdatedAt = null;
        promotion.IsDeleted = false;

        // 4. Nếu là loại khuyến mãi trực tiếp trên sản phẩm (DISCOUNT)
        if (request.ProductPromotions != null && request.ProductPromotions.Any())
        {
            var productIds = request.ProductPromotions.Select(p => p.ProductId).Distinct().ToList();
            var products = await _unitOfWork.Products.GetByIdsAsync(productIds, cancellationToken);

            if (products.Count != productIds.Count)
            {
                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "One or more products do not exist.");
            }

            foreach (var pp in request.ProductPromotions)
            {
                var product = products.First(p => p.ProductId == pp.ProductId);
                // Đảm bảo giá khuyến mãi không lớn hơn giá gốc của sản phẩm
                if (pp.SalePrice > product.Price)
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", $"Sale price for product {product.ProductName} cannot be greater than original price ({product.Price}).");
                }

                var productPromotion = _mapper.Map<ProductPromotion>(pp);
                productPromotion.CreatedAt = _timeProvider.UtcNow;
                promotion.ProductPromotions.Add(productPromotion);
            }
        }

        // 5. Nếu là loại khuyến mãi Flash Sale theo khung giờ (FLASH_SALE)
        if (request.PromotionTimeSlots != null && request.PromotionTimeSlots.Any())
        {
            foreach (var ts in request.PromotionTimeSlots)
            {
                var timeSlot = _mapper.Map<PromotionTimeSlot>(ts);
                timeSlot.CreatedAt = _timeProvider.UtcNow;
                promotion.PromotionTimeSlots.Add(timeSlot);
            }
        }

        // 6. Thực thi lưu xuống Database trong Transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Promotions.AddAsync(promotion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create promotion {PromotionName}", promotion.PromotionName);
            throw;
        }

        _logger.LogInformation(
            "Created promotion with name {PromotionName}",
            promotion.PromotionName);

        // 7. Nạp lại thông tin đầy đủ của chương trình vừa tạo để ánh xạ sang DTO trả về
        var createdPromotion = await _unitOfWork.Promotions.GetByIdAsync(
            promotion.PromotionId,
            cancellationToken,
            includeProperties: "ProductPromotions,ProductPromotions.Product,PromotionTimeSlots,PromotionTimeSlots.PromotionProductSlots,PromotionTimeSlots.PromotionProductSlots.Product"
        );
        var dto = _mapper.Map<PromotionDto>(createdPromotion ?? promotion);

        // 8. Thông báo cho CartService cập nhật lại giá khuyến mãi của các sản phẩm bị ảnh hưởng trong giỏ hàng
        var createdProductIds = (createdPromotion ?? promotion).ProductPromotions
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();
        if (createdProductIds.Count > 0)
        {
            await _cartService.NotifyPromotionChangedAsync(createdProductIds, cancellationToken);
        }

        return Result<PromotionDto>.Success(dto);
    }

    /// <summary>
    /// Cập nhật thông tin chương trình khuyến mãi theo ID (áp dụng các cơ chế bảo vệ giao dịch, Partial Update và kiểm tra trạng thái).
    /// </summary>
    /// <param name="promotionId">Mã ID chương trình khuyến mãi cần cập nhật.</param>
    /// <param name="request">DTO chứa các trường cần cập nhật.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa PromotionDto chi tiết sau cập nhật.</returns>
    public async Task<Result<PromotionDto>> UpdatePromotionAsync(
        int promotionId,
        UpdatePromotionDto request,
        CancellationToken cancellationToken = default)
    {
        if (promotionId <= 0)
        {
            return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Promotion ID must be greater than 0.");
        }

        // 1. Kiểm tra tính hợp lệ của DTO cập nhật qua FluentValidation
        var updateValidation = await _updateValidator.ValidateAsync(request, cancellationToken);

        if (!updateValidation.IsValid)
        {
            return updateValidation.ToResult<PromotionDto>();
        }

        // 2. Tìm nạp thực thể chương trình khuyến mãi hiện tại
        var existingPromotion = await _unitOfWork.Promotions.GetByIdAsync(
            promotionId,
            cancellationToken,
            includeProperties: "ProductPromotions,PromotionTimeSlots,PromotionTimeSlots.PromotionProductSlots"
        );
        if (existingPromotion is null)
        {
            return Result<PromotionDto>.NotFound("Promotion", promotionId);
        }

        // 3. Xử lý yêu cầu Xóa mềm (Soft Delete)
        if (request.IsDeleted == true)
        {
            // Không cho phép xóa chương trình đang ở trạng thái Active
            if (string.Equals(existingPromotion.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot delete an Active promotion.");
            }

            existingPromotion.IsDeleted = true;
            existingPromotion.UpdatedAt = _timeProvider.UtcNow;

            // Chuẩn hóa dữ liệu cũ để tránh vi phạm CHECK constraint trong SQL Server khi xóa mềm
            if (existingPromotion.StartDate == default)
            {
                existingPromotion.StartDate = _timeProvider.UtcNow;
            }
            if (existingPromotion.EndDate <= existingPromotion.StartDate)
            {
                existingPromotion.EndDate = existingPromotion.StartDate.AddDays(1);
            }
        }
        else
        {
            var oldStatus = existingPromotion.Status;
            var now = _timeProvider.UtcNow;
            // Kiểm tra xem đã có giao dịch mua hàng nào trong các khung giờ flash sale hay chưa
            bool hasTransactions = existingPromotion.PromotionTimeSlots.Any(ts => ts.PromotionProductSlots.Any(pps => pps.SoldQuantity > 0));

            // Kiểm tra trường hợp đặc biệt: chương trình Active nhưng rỗng (chưa gán sản phẩm)
            bool isEmptyActivePromotion = string.Equals(oldStatus, "Active", StringComparison.OrdinalIgnoreCase) &&
                ((string.Equals(existingPromotion.PromotionType, "FLASH_SALE", StringComparison.OrdinalIgnoreCase) && 
                  !existingPromotion.PromotionTimeSlots.Any(ts => !ts.IsDeleted && ts.PromotionProductSlots.Any(pps => !pps.IsDeleted))) ||
                 (string.Equals(existingPromotion.PromotionType, "DISCOUNT", StringComparison.OrdinalIgnoreCase) && !existingPromotion.ProductPromotions.Any(p => !p.IsDeleted)));

            // Không cho phép chỉnh sửa chương trình khuyến mãi đã hết hạn (Expired)
            if (string.Equals(oldStatus, "Expired", StringComparison.OrdinalIgnoreCase))
            {
                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot update an Expired promotion.");
            }

            // 4. Áp dụng các chốt an toàn (Safeguards) khi chương trình đang Active hoặc đã phát sinh giao dịch mua hàng
            if (hasTransactions || (string.Equals(oldStatus, "Active", StringComparison.OrdinalIgnoreCase) && !isEmptyActivePromotion))
            {
                // Không được thay đổi loại khuyến mãi (PromotionType)
                if (request.PromotionType is not null && !string.Equals(request.PromotionType, existingPromotion.PromotionType, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot modify PromotionType for a promotion that is active or has transactions.");
                }

                // Không được thêm/bớt sản phẩm hoặc đổi giá khuyến mãi trong ProductPromotions
                if (request.ProductPromotions is not null)
                {
                    var existingActiveProducts = existingPromotion.ProductPromotions.Where(p => !p.IsDeleted).ToList();
                    var incomingProductIds = request.ProductPromotions.Select(p => p.ProductId).ToList();
                    var existingProductIds = existingActiveProducts.Select(p => p.ProductId).ToList();

                    if (incomingProductIds.Count != existingProductIds.Count || !incomingProductIds.All(existingProductIds.Contains))
                    {
                        return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot add or remove products for a promotion that is active or has transactions.");
                    }

                    foreach (var incomingPp in request.ProductPromotions)
                    {
                        var existingPp = existingActiveProducts.First(p => p.ProductId == incomingPp.ProductId);
                        if (incomingPp.SalePrice != existingPp.SalePrice)
                        {
                            return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot modify sale price for products in a promotion that is active or has transactions.");
                        }
                    }
                }

                // Không được thêm/bớt khung giờ hoặc đổi giá/số lượng sản phẩm trong PromotionTimeSlots
                if (request.PromotionTimeSlots is not null)
                {
                    var existingActiveTimeSlots = existingPromotion.PromotionTimeSlots.Where(ts => !ts.IsDeleted).ToList();

                    if (request.PromotionTimeSlots.Count != existingActiveTimeSlots.Count)
                    {
                        return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot add or remove time slots for a promotion that is active or has transactions.");
                    }

                    foreach (var incomingTs in request.PromotionTimeSlots)
                    {
                        var existingTs = existingActiveTimeSlots.FirstOrDefault(ts => ts.StartAt == incomingTs.StartAt);
                        if (existingTs is null)
                        {
                            int idx = request.PromotionTimeSlots.IndexOf(incomingTs);
                            if (idx >= 0 && idx < existingActiveTimeSlots.Count)
                            {
                                existingTs = existingActiveTimeSlots[idx];
                            }
                        }

                        if (existingTs is not null)
                        {
                            var existingSlotProducts = existingTs.PromotionProductSlots.Where(p => !p.IsDeleted).ToList();
                            var incomingSlotProductIds = incomingTs.PromotionProductSlots.Select(p => p.ProductId).ToList();
                            var existingSlotProductIds = existingSlotProducts.Select(p => p.ProductId).ToList();

                            if (incomingSlotProductIds.Count != existingSlotProductIds.Count || !incomingSlotProductIds.All(existingSlotProductIds.Contains))
                            {
                                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot add or remove products in time slots for a promotion that is active or has transactions.");
                            }

                            foreach (var incomingPs in incomingTs.PromotionProductSlots)
                            {
                                var existingPs = existingSlotProducts.First(p => p.ProductId == incomingPs.ProductId);
                                if (incomingPs.SalePrice != existingPs.SalePrice || incomingPs.SaleQuantity != existingPs.SaleQuantity)
                                {
                                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot modify sale price or sale quantity in time slots for a promotion that is active or has transactions.");
                                }
                            }
                        }
                    }
                }
            }

            // 5. Kiểm tra tính hợp lệ của ngày bắt đầu và kết thúc mục tiêu
            var targetStatus = request.Status ?? oldStatus;
            var targetStartDate = request.StartDate ?? existingPromotion.StartDate;
            var targetEndDate = request.EndDate ?? existingPromotion.EndDate;

            if (targetStartDate >= targetEndDate)
            {
                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Start date must be earlier than end date.");
            }

            if (string.Equals(targetStatus, "Active", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetStatus, "Scheduled", StringComparison.OrdinalIgnoreCase))
            {
                if (targetEndDate <= now)
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot activate or schedule a promotion with an end date in the past. Please extend the End Date.");
                }
            }

            // 6. Kiểm tra các bước chuyển trạng thái (State Machine Transitions)
            if (string.Equals(oldStatus, "Active", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(targetStatus, "Active", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(targetStatus, "Inactive", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Active promotion can only be changed to Inactive or remain Active.");
                }
            }
            else if (string.Equals(oldStatus, "Inactive", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(targetStatus, "Inactive", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(targetStatus, "Scheduled", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(targetStatus, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Inactive promotion can only be rescheduled/reactivated or remain Inactive.");
                }
            }
            else if (string.Equals(oldStatus, "Scheduled", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(targetStatus, "Scheduled", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(targetStatus, "Active", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(targetStatus, "Inactive", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Scheduled promotion can only be changed to Active, Inactive, or remain Scheduled.");
                }
            }

            // 7. Chuẩn hóa StartDate và Status khi kích hoạt lại hoặc đổi lịch
            if (string.Equals(targetStatus, "Scheduled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetStatus, "Active", StringComparison.OrdinalIgnoreCase))
            {
                if (targetStartDate <= now && string.Equals(oldStatus, "Scheduled", StringComparison.OrdinalIgnoreCase))
                {
                    existingPromotion.StartDate = now;
                    existingPromotion.Status = "Active";
                }
                else
                {
                    existingPromotion.StartDate = targetStartDate;
                    existingPromotion.Status = targetStatus;
                }
            }
            else
            {
                existingPromotion.Status = targetStatus;
                if (request.StartDate.HasValue)
                {
                    existingPromotion.StartDate = request.StartDate.Value;
                }
            }

            existingPromotion.EndDate = targetEndDate;

            if (request.PromotionType is not null && !hasTransactions && !string.Equals(oldStatus, "Active", StringComparison.OrdinalIgnoreCase))
            {
                existingPromotion.PromotionType = request.PromotionType;
            }

            if (request.PromotionName is not null)
            {
                existingPromotion.PromotionName = request.PromotionName;
                var promotionNameExists = await _unitOfWork.Promotions.ExistsPromotionNameAsync(
                    existingPromotion.PromotionName,
                    promotionId,
                    cancellationToken);

                if (promotionNameExists)
                {
                    return Result<PromotionDto>.Conflict("Promotion name already exists.");
                }
            }

            if (request.Description is not null)
            {
                existingPromotion.Description = request.Description;
            }

            if (request.Priority.HasValue)
            {
                existingPromotion.Priority = request.Priority.Value;
            }

            existingPromotion.UpdatedAt = now;

            // 8. Cập nhật danh sách sản phẩm khuyến mãi (ProductPromotions)
            if (request.ProductPromotions != null)
            {
                var productIds = request.ProductPromotions.Select(p => p.ProductId).Distinct().ToList();
                var products = await _unitOfWork.Products.GetByIdsAsync(productIds, cancellationToken);

                if (products.Count != productIds.Count)
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "One or more products do not exist.");
                }

                var incomingProductIds = request.ProductPromotions.Select(p => p.ProductId).ToList();
                var toRemove = existingPromotion.ProductPromotions
                    .Where(pp => !incomingProductIds.Contains(pp.ProductId))
                    .ToList();

                // Đánh dấu xóa mềm cho sản phẩm bị loại bỏ khỏi khuyến mãi
                foreach (var item in toRemove)
                {
                    item.IsDeleted = true;
                    item.UpdatedAt = _timeProvider.UtcNow;
                }

                // Thêm mới hoặc cập nhật các sản phẩm trong danh sách gửi lên
                foreach (var incomingPp in request.ProductPromotions)
                {
                    var product = products.First(p => p.ProductId == incomingPp.ProductId);
                    if (incomingPp.SalePrice > product.Price)
                    {
                        return Result<PromotionDto>.Failure("VALIDATION_ERROR", $"Sale price for product {product.ProductName} cannot be greater than original price ({product.Price}).");
                    }

                    var existingPp = existingPromotion.ProductPromotions
                        .FirstOrDefault(pp => pp.ProductId == incomingPp.ProductId);

                    if (existingPp != null)
                    {
                        existingPp.SalePrice = incomingPp.SalePrice;
                        existingPp.DiscountPercent = incomingPp.DiscountPercent;
                        existingPp.UpdatedAt = _timeProvider.UtcNow;
                        existingPp.IsDeleted = false;
                    }
                    else
                    {
                        var newPp = _mapper.Map<ProductPromotion>(incomingPp);
                        newPp.PromotionId = promotionId;
                        newPp.CreatedAt = _timeProvider.UtcNow;
                        existingPromotion.ProductPromotions.Add(newPp);
                    }
                }
            }

            // 9. Cập nhật danh sách khung giờ Flash Sale (PromotionTimeSlots)
            if (request.PromotionTimeSlots != null)
            {
                var existingTimeSlots = existingPromotion.PromotionTimeSlots.ToList();
                var incomingTimeSlots = request.PromotionTimeSlots.ToList();
                var incomingStartTimes = incomingTimeSlots.Select(ts => ts.StartAt).ToList();

                var toRemoveSlots = existingTimeSlots
                    .Where(ts => !incomingStartTimes.Contains(ts.StartAt))
                    .ToList();

                foreach (var item in toRemoveSlots)
                {
                    if (item.Status == "Active" || item.Status == "Inactive" || item.Status == "Expired")
                    {
                        return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot delete an Active, Inactive, or Expired time slot.");
                    }
                    item.IsDeleted = true;
                    item.UpdatedAt = _timeProvider.UtcNow;
                }

                foreach (var incomingTs in incomingTimeSlots)
                {
                    var existingTs = existingPromotion.PromotionTimeSlots.FirstOrDefault(ts => ts.StartAt == incomingTs.StartAt);
                    if (existingTs != null)
                    {
                        if (existingTs.Status == "Active")
                        {
                            if (incomingTs.Status != "Active" && incomingTs.Status != "Inactive")
                            {
                                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Active time slot can only be changed to Inactive.");
                            }

                            if (existingTs.Status != incomingTs.Status)
                            {
                                existingTs.Status = incomingTs.Status;
                                existingTs.UpdatedAt = _timeProvider.UtcNow;
                            }
                            continue;
                        }
                        else if (existingTs.Status == "Inactive" || existingTs.Status == "Expired")
                        {
                            if (existingTs.Status != incomingTs.Status)
                            {
                                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot modify the status of an Inactive or Expired time slot.");
                            }
                            continue;
                        }

                        existingTs.EndAt = incomingTs.EndAt;
                        existingTs.Status = incomingTs.Status;
                        existingTs.UpdatedAt = _timeProvider.UtcNow;
                        existingTs.IsDeleted = false;

                        if (incomingTs.PromotionProductSlots != null)
                        {
                            var incomingProductIds = incomingTs.PromotionProductSlots.Select(p => p.ProductId).ToList();
                            var toRemoveProducts = existingTs.PromotionProductSlots
                                .Where(p => !incomingProductIds.Contains(p.ProductId))
                                .ToList();

                            foreach (var rp in toRemoveProducts)
                            {
                                rp.IsDeleted = true;
                                rp.UpdatedAt = _timeProvider.UtcNow;
                            }

                            foreach (var incomingP in incomingTs.PromotionProductSlots)
                            {
                                var existingP = existingTs.PromotionProductSlots.FirstOrDefault(p => p.ProductId == incomingP.ProductId);
                                if (existingP != null)
                                {
                                    existingP.SalePrice = incomingP.SalePrice;
                                    existingP.DiscountPercent = incomingP.DiscountPercent;
                                    existingP.SaleQuantity = incomingP.SaleQuantity;
                                    existingP.UpdatedAt = _timeProvider.UtcNow;
                                    existingP.IsDeleted = false;
                                }
                                else
                                {
                                    var newP = _mapper.Map<PromotionProductSlot>(incomingP);
                                    newP.CreatedAt = _timeProvider.UtcNow;
                                    existingTs.PromotionProductSlots.Add(newP);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (incomingTs.Status == "Scheduled" && incomingTs.StartAt < _timeProvider.UtcNow.AddMinutes(9))
                        {
                            return Result<PromotionDto>.Failure("VALIDATION_ERROR", "New time slot start time must be at least 10 minutes from now.");
                        }

                        var newTs = _mapper.Map<PromotionTimeSlot>(incomingTs);
                        newTs.PromotionId = promotionId;
                        newTs.CreatedAt = _timeProvider.UtcNow;
                        existingPromotion.PromotionTimeSlots.Add(newTs);
                    }
                }
            }

            // 10. Chạy toàn bộ bộ quy tắc kiểm tra tính hợp lệ trên trạng thái tổng thể cuối cùng của chương trình
            var fullValidationRequest = _mapper.Map<CreatePromotionDto>(existingPromotion);

            var context = new FluentValidation.ValidationContext<CreatePromotionDto>(fullValidationRequest);
            context.RootContextData["IsUpdate"] = true;

            var fullValidationResult = await _createValidator.ValidateAsync(context, cancellationToken);

            if (!fullValidationResult.IsValid)
            {
                return fullValidationResult.ToResult<PromotionDto>();
            }
        }

        // 11. Lưu thay đổi vào Database trong Transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update promotion {PromotionId}", promotionId);
            throw;
        }

        // 12. Nạp lại dữ liệu sau cập nhật để trả về cho Client
        var updatedPromotion = await _unitOfWork.Promotions.GetByIdAsync(
            promotionId,
            cancellationToken,
            includeProperties: "ProductPromotions,ProductPromotions.Product,PromotionTimeSlots,PromotionTimeSlots.PromotionProductSlots,PromotionTimeSlots.PromotionProductSlots.Product"
        ) ?? existingPromotion;

        _logger.LogInformation(
            "Updated promotion {PromotionId} with name {PromotionName}",
            promotionId,
            updatedPromotion.PromotionName);

        // 13. Thông báo cho CartService đồng bộ lại giá các sản phẩm bị ảnh hưởng trong giỏ hàng
        var impactedProductIds = updatedPromotion.ProductPromotions
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();
        if (impactedProductIds.Count > 0)
        {
            await _cartService.NotifyPromotionChangedAsync(impactedProductIds, cancellationToken);
        }

        return Result<PromotionDto>.Success(_mapper.Map<PromotionDto>(updatedPromotion));
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các chương trình khuyến mãi (cả DISCOUNT thông thường và FLASH_SALE theo khung giờ) đang áp dụng cho một sản phẩm cụ thể.
    /// </summary>
    /// <param name="productId">Mã ID sản phẩm cần kiểm tra.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa danh sách ProductPromotionInfoDto.</returns>
    public async Task<Result<List<ProductPromotionInfoDto>>> GetPromotionsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
        {
            return Result<List<ProductPromotionInfoDto>>.Failure("VALIDATION_ERROR", "Product ID must be greater than 0.");
        }

        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result<List<ProductPromotionInfoDto>>.NotFound("Product", productId);
        }

        var list = await _unitOfWork.Promotions.GetPromotionsByProductIdAsync(productId, cancellationToken);
        return Result<List<ProductPromotionInfoDto>>.Success(list);
    }
}
