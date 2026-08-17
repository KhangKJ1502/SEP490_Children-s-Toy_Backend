using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Vouchers;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Application.Common.Models;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Service triển khai các nghiệp vụ quản lý Voucher (mã khuyến mãi/giảm giá), bao gồm:
/// - Tạo mới, cập nhật từng phần, xem chi tiết, phân trang danh sách voucher.
/// - Kiểm tra ràng buộc và phân luồng trạng thái duyệt tự động theo phân quyền (Admin / Staff).
/// - Áp dụng các chốt an toàn (Safeguards) bảo vệ dữ liệu khi voucher đã có lượt sử dụng hoặc đã hết hạn.
/// </summary>
public class VoucherService : IVoucherService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<VoucherService> _logger;
    private readonly IValidator<CreateVoucherDto> _createValidator;
    private readonly IValidator<UpdateVoucherDto> _updateValidator;
    private readonly ITimeProvider _timeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly VoucherRiskThresholds _thresholds;

    /// <summary>
    /// Khởi tạo VoucherService với các dependency cần thiết.
    /// </summary>
    /// <param name="unitOfWork">Unit of Work quản lý các repository và transaction cơ sở dữ liệu.</param>
    /// <param name="mapper">AutoMapper để chuyển đổi giữa Entity và DTO.</param>
    /// <param name="logger">Logger ghi lại log hệ thống.</param>
    /// <param name="createValidator">Bộ validator kiểm tra dữ liệu tạo mới voucher.</param>
    /// <param name="updateValidator">Bộ validator kiểm tra dữ liệu cập nhật voucher.</param>
    /// <param name="timeProvider">Provider cung cấp thời gian hiện tại (hỗ trợ giả lập/kiểm thử thời gian).</param>
    /// <param name="currentUserService">Service cung cấp thông tin người dùng đang đăng nhập (AccountId, RoleName).</param>
    /// <param name="thresholds">Ngưỡng rủi ro cấu hình để quyết định voucher của Staff có cần duyệt hay không.</param>
    public VoucherService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<VoucherService> logger,
        IValidator<CreateVoucherDto> createValidator,
        IValidator<UpdateVoucherDto> updateValidator,
        ITimeProvider timeProvider,
        ICurrentUserService currentUserService,
        IOptions<VoucherRiskThresholds> thresholds)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _timeProvider = timeProvider;
        _currentUserService = currentUserService;
        _thresholds = thresholds.Value;
    }

    /// <summary>
    /// Lấy danh sách voucher có phân trang, lọc và sắp xếp.
    /// Nếu người dùng đã đăng nhập, tự động tính số lần người dùng đó đã dùng từng voucher (CurrentUserUsageCount).
    /// </summary>
    /// <param name="pageNumber">Số trang hiện tại (>= 1).</param>
    /// <param name="pageSize">Số lượng bản ghi mỗi trang (1 đến 100).</param>
    /// <param name="sortBy">Tên trường sắp xếp.</param>
    /// <param name="sortDesc">true: giảm dần, false: tăng dần.</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm theo mã, tên hoặc mô tả.</param>
    /// <param name="status">Trạng thái voucher cần lọc.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa danh sách VoucherListDto đã phân trang.</returns>
    public async Task<Result<PaginatedResponse<VoucherListDto>>> GetVouchersAsync(
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
            return Result<PaginatedResponse<VoucherListDto>>.Failure(
                "VALIDATION_ERROR", "Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<VoucherListDto>>.Failure(
                "VALIDATION_ERROR", "Page size must be between 1 and 100.");
        }

        // 2. Truy vấn danh sách voucher từ repository
        var pagedVouchers = await _unitOfWork.Vouchers.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            status,
            cancellationToken);

        // 3. Map danh sách thực thể sang danh sách DTO gọn (VoucherListDto)
        var items = _mapper.Map<List<VoucherListDto>>(pagedVouchers.Items);

        // 4. Nếu người dùng hiện tại đã đăng nhập (AccountId > 0), tính số lần người dùng này đã dùng voucher
        var accountId = _currentUserService.AccountId;
        if (accountId > 0)
        {
            foreach (var item in items)
            {
                if (item.MaxUsagePerUser.HasValue)
                {
                    item.CurrentUserUsageCount = await _unitOfWork.Vouchers.CountUsageByAccountAsync(item.VoucherId, accountId, cancellationToken);
                }
            }
        }

        _logger.LogInformation(
            "Retrieved vouchers list with PageNumber {PageNumber}, PageSize {PageSize}, SearchTerm {SearchTerm}, Status {Status}",
            pageNumber,
            pageSize,
            searchTerm,
            status);

        // 5. Đóng gói kết quả phân trang và trả về
        var response = new PaginatedResponse<VoucherListDto>(
            items,
            pagedVouchers.TotalCount,
            pageNumber,
            pageSize);

        return Result<PaginatedResponse<VoucherListDto>>.Success(response);
    }

    /// <summary>
    /// Tạo mới một voucher:
    /// - Chuẩn hóa dữ liệu đầu vào (Trim, ToUpper mã code và loại giảm giá).
    /// - Kiểm tra hợp lệ dữ liệu qua FluentValidation.
    /// - Kiểm tra trùng lặp mã VoucherCode.
    /// - Phân luồng trạng thái duyệt (Status Transition Rules):
    ///   + Admin: Trực tiếp vào trạng thái Scheduled.
    ///   + Staff: Nếu giảm giá trên giá cuối (FINAL_PRICE), không giới hạn số lượng (TotalQuantity = null), hoặc vượt ngưỡng rủi ro (MaxDiscountCap, MaxTotalDiscount) -> chuyển sang Pending chờ duyệt; ngược lại vào Scheduled.
    /// - Lưu xuống database trong Transaction an toàn.
    /// </summary>
    /// <param name="request">DTO chứa thông tin tạo voucher.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa VoucherDto chi tiết của voucher vừa tạo.</returns>
    public async Task<Result<VoucherDto>> CreateVoucherAsync(
        CreateVoucherDto request,
        CancellationToken cancellationToken = default)
    {
        // 1. Chuẩn hoá input trước khi validate (cắt khoảng trắng, viết hoa mã code/loại giảm giá)
        var normalizedRequest = NormalizeCreateRequest(request);
        var validationResult = await _createValidator.ValidateAsync(normalizedRequest, cancellationToken);

        if (!validationResult.IsValid)
        {
            return validationResult.ToResult<VoucherDto>();
        }

        // 2. Kiểm tra trùng mã voucher code trong cơ sở dữ liệu
        var voucherCodeExists = await _unitOfWork.Vouchers.ExistsVoucherCodeAsync(
            normalizedRequest.VoucherCode,
            null,
            cancellationToken);

        if (voucherCodeExists)
        {
            return Result<VoucherDto>.Conflict("Voucher code already exists.");
        }

        // 3. Khởi tạo đối tượng Entity Voucher từ DTO và gán thông tin hệ thống
        var voucher = _mapper.Map<Voucher>(normalizedRequest);
        voucher.CreatedBy = _currentUserService.AccountId;
        voucher.CreatedAt = _timeProvider.UtcNow;
        voucher.UpdatedAt = null;
        voucher.UsedQuantity = 0;
        voucher.IsDeleted = false;

        // 4. Áp dụng quy tắc chuyển đổi trạng thái duyệt (Status Transition Rules)
        if (string.Equals(_currentUserService.RoleName, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            // Nhóm 1: Voucher áp dụng trên giá cuối (FINAL_PRICE) luôn cần Admin duyệt
            if (string.Equals(voucher.DiscountTarget, "FINAL_PRICE", StringComparison.OrdinalIgnoreCase))
            {
                voucher.Status = VoucherStatuses.Pending;
            }
            // Nhóm 2: Voucher theo phần trăm (%) - kiểm tra mức giảm tối đa (cap) và tổng ngân sách giảm giá
            else if (string.Equals(voucher.DiscountType, "PERCENTAGE", StringComparison.OrdinalIgnoreCase))
            {
                // Nếu Staff không giới hạn số lượng (TotalQuantity = null) -> ngân sách vô hạn -> chuyển sang Pending chờ Admin duyệt
                bool isUnlimited = !voucher.TotalQuantity.HasValue;
                bool exceedsDiscountCap = (voucher.MaxDiscountCap ?? 0) > _thresholds.MaxDiscountCap;
                bool exceedsTotalBudget = isUnlimited || ((voucher.MaxDiscountCap ?? 0) * voucher.TotalQuantity!.Value) > _thresholds.MaxTotalDiscount;

                if (exceedsDiscountCap || exceedsTotalBudget)
                {
                    voucher.Status = VoucherStatuses.Pending;
                }
                else
                {
                    voucher.Status = VoucherStatuses.Scheduled;
                }
            }
            // Nhóm 3: Voucher giảm theo số tiền cố định (FIXED) - kiểm tra số tiền giảm và tổng ngân sách
            else if (string.Equals(voucher.DiscountType, "FIXED", StringComparison.OrdinalIgnoreCase))
            {
                // Nếu Staff không giới hạn số lượng (TotalQuantity = null) -> ngân sách vô hạn -> chuyển sang Pending chờ Admin duyệt
                bool isUnlimited = !voucher.TotalQuantity.HasValue;
                bool exceedsDiscountCap = voucher.DiscountValue > _thresholds.MaxDiscountCap;
                bool exceedsTotalBudget = isUnlimited || (voucher.DiscountValue * voucher.TotalQuantity!.Value) > _thresholds.MaxTotalDiscount;

                if (exceedsDiscountCap || exceedsTotalBudget)
                {
                    voucher.Status = VoucherStatuses.Pending;
                }
                else
                {
                    voucher.Status = VoucherStatuses.Scheduled;
                }
            }
            else
            {
                voucher.Status = VoucherStatuses.Scheduled;
            }
        }
        else // Tài khoản Admin tạo thì mặc định được lên lịch (Scheduled) ngay không cần duyệt
        {
            voucher.Status = VoucherStatuses.Scheduled;
        }

        // 5. Mở Transaction và lưu vào database
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Vouchers.AddAsync(voucher, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create voucher with code {VoucherCode}", voucher.VoucherCode);
            throw;
        }

        // 6. Truy vấn lại voucher vừa tạo để đảm bảo dữ liệu chuẩn xác trước khi trả về
        var createdVoucher = await _unitOfWork.Vouchers.GetByCodeAsync(voucher.VoucherCode, cancellationToken);
        if (createdVoucher is null)
        {
            _logger.LogError("Failed to load created voucher by code {VoucherCode}", voucher.VoucherCode);
            return Result<VoucherDto>.Failure("ERROR", "Failed to create voucher.");
        }

        _logger.LogInformation(
            "Created voucher {VoucherId} with code {VoucherCode}",
            createdVoucher.VoucherId,
            createdVoucher.VoucherCode);

        return Result<VoucherDto>.Success(_mapper.Map<VoucherDto>(createdVoucher));
    }

    /// <summary>
    /// Cập nhật thông tin voucher theo ID (hỗ trợ cập nhật từng phần - Partial Update):
    /// - Kiểm tra ID hợp lệ và sự tồn tại của voucher.
    /// - Xử lý xóa mềm (Soft Delete): không cho xóa voucher đang Active, xử lý dữ liệu cũ để tránh lỗi CHECK constraint SQL Server.
    /// - Safeguard 1 (Voucher đã sử dụng): Chặn sửa các trường tài chính cốt lõi (Mã, Loại giảm giá, Giá trị giảm, Mức giảm tối đa, Mục tiêu giảm giá, Đơn tối thiểu); không cho giảm Tổng số lượng nhỏ hơn số lượng đã dùng.
    /// - Safeguard 2 (Voucher đã hết hạn): Khóa voucher đã hết hạn, chỉ cho phép gia hạn EndDate hoặc chuyển trạng thái sang Inactive/xóa để lưu trữ.
    /// - Rule 1: Nếu Staff sửa trường tài chính của voucher đang Active/Scheduled -> tự động chuyển về Pending chờ duyệt lại.
    /// - Role-based Logic: Staff không được tự ý duyệt; Admin từ chối phải nhập Reason; Admin duyệt sẽ tính toán trạng thái Active/Scheduled theo ngày hiệu lực.
    /// - Rule 2: Chặn kích hoạt lại voucher Inactive nếu đã hết hạn hoặc đã đạt giới hạn số lượng sử dụng.
    /// - Merge dữ liệu an toàn, validate toàn diện lại một lần nữa trước khi commit transaction.
    /// </summary>
    /// <param name="voucherId">Mã ID của voucher cần cập nhật.</param>
    /// <param name="request">DTO chứa các trường cần cập nhật.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa VoucherDto sau khi cập nhật thành công.</returns>
    public async Task<Result<VoucherDto>> UpdateVoucherAsync(
        int voucherId,
        UpdateVoucherDto request,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra ID voucher hợp lệ
        if (voucherId <= 0)
        {
            return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Voucher ID must be greater than 0.");
        }

        // 2. Chuẩn hóa và validate các trường trong request cập nhật từng phần
        var normalizedRequest = NormalizeUpdateRequest(request);
        var updateValidation = await _updateValidator.ValidateAsync(normalizedRequest, cancellationToken);

        if (!updateValidation.IsValid)
        {
            return updateValidation.ToResult<VoucherDto>();
        }

        // 3. Tìm kiếm voucher hiện tại trong DB
        var existingVoucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId, cancellationToken);
        if (existingVoucher is null)
        {
            return Result<VoucherDto>.NotFound("Voucher", voucherId);
        }

        // 4. Xử lý trường hợp yêu cầu Xóa mềm (Soft Delete)
        if (normalizedRequest.IsDeleted == true)
        {
            // Không cho phép xóa voucher đang trong trạng thái Active (đang hoạt động)
            if (string.Equals(existingVoucher.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Cannot delete an Active voucher.");
            }

            existingVoucher.IsDeleted = true;
            existingVoucher.UpdatedAt = _timeProvider.UtcNow;

            // Xử lý các giá trị không hợp lệ từ dữ liệu cũ (legacy data) để thỏa mãn CHECK constraint của SQL Server khi xóa mềm
            if (existingVoucher.DiscountValue <= 0)
            {
                existingVoucher.DiscountValue = 1;
            }
            if (existingVoucher.StartDate == default)
            {
                existingVoucher.StartDate = _timeProvider.UtcNow;
            }
            if (existingVoucher.EndDate <= existingVoucher.StartDate)
            {
                existingVoucher.EndDate = existingVoucher.StartDate.AddDays(1);
            }
        }
        else
        {
            var oldStatus = existingVoucher.Status;
            var now = _timeProvider.UtcNow;

            // ── Chốt an toàn 1: Safeguard cho Voucher đã có lượt sử dụng (UsedQuantity > 0) ──
            if (existingVoucher.UsedQuantity > 0)
            {
                // Kiểm tra xem có yêu cầu thay đổi các trường tài chính cốt lõi hay không
                bool hasInvalidCriticalChanges =
                    (normalizedRequest.VoucherCode is not null && !string.Equals(normalizedRequest.VoucherCode, existingVoucher.VoucherCode, StringComparison.OrdinalIgnoreCase)) ||
                    (normalizedRequest.DiscountType is not null && !string.Equals(normalizedRequest.DiscountType, existingVoucher.DiscountType, StringComparison.OrdinalIgnoreCase)) ||
                    (normalizedRequest.DiscountValue.HasValue && normalizedRequest.DiscountValue.Value != existingVoucher.DiscountValue) ||
                    (normalizedRequest.MaxDiscountCap.HasValue && normalizedRequest.MaxDiscountCap.Value != existingVoucher.MaxDiscountCap) ||
                    (normalizedRequest.DiscountTarget is not null && !string.Equals(normalizedRequest.DiscountTarget, existingVoucher.DiscountTarget, StringComparison.OrdinalIgnoreCase)) ||
                    (normalizedRequest.MinOrderAmount.HasValue && normalizedRequest.MinOrderAmount.Value != existingVoucher.MinOrderAmount);

                if (hasInvalidCriticalChanges)
                {
                    return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Cannot modify core financial fields (VoucherCode, DiscountType, DiscountValue, MaxDiscountCap, DiscountTarget, MinOrderAmount) for a voucher that has already been used.");
                }

                // Không cho phép chỉnh Tổng số lượng (TotalQuantity) nhỏ hơn số lượng đã dùng thực tế
                if (normalizedRequest.TotalQuantity.HasValue && normalizedRequest.TotalQuantity.Value < existingVoucher.UsedQuantity)
                {
                    return Result<VoucherDto>.Failure("VALIDATION_ERROR", $"Total quantity cannot be set below the used quantity ({existingVoucher.UsedQuantity}).");
                }
            }

            // ── Chốt an toàn 2: Safeguard cho Voucher đã hết hạn (Expired) ──
            bool isExpired = string.Equals(oldStatus, VoucherStatuses.Expired, StringComparison.OrdinalIgnoreCase) || existingVoucher.EndDate <= now;
            if (isExpired)
            {
                // Voucher đã hết hạn bị khóa toàn bộ các thông tin, ngoại trừ EndDate, Status hoặc IsDeleted
                bool hasInvalidExpiredChanges =
                    (normalizedRequest.VoucherCode is not null && !string.Equals(normalizedRequest.VoucherCode, existingVoucher.VoucherCode, StringComparison.OrdinalIgnoreCase)) ||
                    (normalizedRequest.VoucherName is not null && !string.Equals(normalizedRequest.VoucherName, existingVoucher.VoucherName, StringComparison.Ordinal)) ||
                    (normalizedRequest.VoucherDescription is not null && !string.Equals(normalizedRequest.VoucherDescription, existingVoucher.VoucherDescription, StringComparison.Ordinal)) ||
                    (normalizedRequest.DiscountType is not null && !string.Equals(normalizedRequest.DiscountType, existingVoucher.DiscountType, StringComparison.OrdinalIgnoreCase)) ||
                    (normalizedRequest.DiscountValue.HasValue && normalizedRequest.DiscountValue.Value != existingVoucher.DiscountValue) ||
                    (normalizedRequest.MaxDiscountCap.HasValue && normalizedRequest.MaxDiscountCap.Value != existingVoucher.MaxDiscountCap) ||
                    (normalizedRequest.DiscountTarget is not null && !string.Equals(normalizedRequest.DiscountTarget, existingVoucher.DiscountTarget, StringComparison.OrdinalIgnoreCase)) ||
                    (normalizedRequest.MinOrderAmount.HasValue && normalizedRequest.MinOrderAmount.Value != existingVoucher.MinOrderAmount) ||
                    (normalizedRequest.TotalQuantity.HasValue && normalizedRequest.TotalQuantity.Value != existingVoucher.TotalQuantity) ||
                    (normalizedRequest.MaxUsagePerUser.HasValue && normalizedRequest.MaxUsagePerUser.Value != existingVoucher.MaxUsagePerUser) ||
                    (normalizedRequest.StartDate.HasValue && normalizedRequest.StartDate.Value != existingVoucher.StartDate);

                if (hasInvalidExpiredChanges)
                {
                    return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Expired vouchers are locked. You can only update the End Date, Status, or delete it to reactivate/archive.");
                }

                // Nếu không phải thao tác lưu trữ (chuyển sang Inactive, Rejected, Expired hoặc Xóa), bắt buộc phải gia hạn EndDate về tương lai
                var targetEndDate = normalizedRequest.EndDate ?? existingVoucher.EndDate;
                var targetStatus = normalizedRequest.Status ?? existingVoucher.Status;
                bool isArchiving = string.Equals(targetStatus, VoucherStatuses.Inactive, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(targetStatus, VoucherStatuses.Rejected, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(targetStatus, VoucherStatuses.Expired, StringComparison.OrdinalIgnoreCase)
                    || normalizedRequest.IsDeleted == true;

                if (!isArchiving && targetEndDate <= now)
                {
                    return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Expired vouchers are locked. Please update the End Date to a future time to reactivate it, or change the status to Inactive/delete it to archive.");
                }
            }

            // ── Quy tắc 1: Staff chỉnh sửa trường tài chính của voucher đang Active hoặc Scheduled ──
            if (string.Equals(_currentUserService.RoleName, "Staff", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(oldStatus, VoucherStatuses.Active, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(oldStatus, VoucherStatuses.Scheduled, StringComparison.OrdinalIgnoreCase)))
            {
                bool hasFinancialChanges =
                    (normalizedRequest.DiscountType is not null && !string.Equals(normalizedRequest.DiscountType, existingVoucher.DiscountType, StringComparison.OrdinalIgnoreCase)) ||
                    (normalizedRequest.DiscountValue.HasValue && normalizedRequest.DiscountValue.Value != existingVoucher.DiscountValue) ||
                    (normalizedRequest.MaxDiscountCap.HasValue && normalizedRequest.MaxDiscountCap.Value != existingVoucher.MaxDiscountCap) ||
                    (normalizedRequest.DiscountTarget is not null && !string.Equals(normalizedRequest.DiscountTarget, existingVoucher.DiscountTarget, StringComparison.OrdinalIgnoreCase)) ||
                    (normalizedRequest.MinOrderAmount.HasValue && normalizedRequest.MinOrderAmount.Value != existingVoucher.MinOrderAmount) ||
                    (normalizedRequest.TotalQuantity.HasValue && normalizedRequest.TotalQuantity.Value != existingVoucher.TotalQuantity) ||
                    (normalizedRequest.StartDate.HasValue && normalizedRequest.StartDate.Value != existingVoucher.StartDate) ||
                    (normalizedRequest.EndDate.HasValue && normalizedRequest.EndDate.Value != existingVoucher.EndDate);

                // Nếu có thay đổi tài chính, bắt buộc đưa về trạng thái Pending để Admin duyệt lại
                if (hasFinancialChanges)
                {
                    normalizedRequest.Status = VoucherStatuses.Pending;
                    normalizedRequest.Reason = null;
                }
            }

            // ── Xử lý phân quyền chi tiết trước khi merge dữ liệu ──
            if (string.Equals(_currentUserService.RoleName, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                // Staff không được phép tự ý đổi trạng thái sang Scheduled hoặc Active (phải qua Pending)
                if (string.Equals(normalizedRequest.Status, VoucherStatuses.Scheduled, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(normalizedRequest.Status, VoucherStatuses.Active, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedRequest.Status = VoucherStatuses.Pending;
                }

                // Nếu Staff chỉnh sửa voucher bị Rejected, tự động chuyển lại về Pending (trừ khi chủ động tắt sang Inactive)
                if (string.Equals(oldStatus, VoucherStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(normalizedRequest.Status, VoucherStatuses.Inactive, StringComparison.OrdinalIgnoreCase))
                    {
                        normalizedRequest.Status = VoucherStatuses.Pending;
                        normalizedRequest.Reason = null; // Xóa lý do từ chối cũ của Admin
                    }
                }

                // Staff không có quyền thay đổi lý do từ chối (Reason)
                normalizedRequest.Reason = null;
            }
            else if (string.Equals(_currentUserService.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Admin từ chối (Rejected) bắt buộc phải nhập lý do từ chối
                if (string.Equals(normalizedRequest.Status, VoucherStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(normalizedRequest.Reason) && string.IsNullOrWhiteSpace(existingVoucher.Reason))
                    {
                        return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Reason is required when rejecting a voucher.");
                    }
                }

                // Khi Admin duyệt (Scheduled): xóa lý do từ chối cũ và tự động xác định trạng thái Active nếu ngày bắt đầu đã đến
                if (string.Equals(normalizedRequest.Status, VoucherStatuses.Scheduled, StringComparison.OrdinalIgnoreCase))
                {
                    var targetEndDate = normalizedRequest.EndDate ?? existingVoucher.EndDate;

                    if (targetEndDate <= now)
                    {
                        return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Cannot approve a voucher that has already expired. Please reject it or ask staff to update the dates.");
                    }

                    var targetStartDate = normalizedRequest.StartDate ?? existingVoucher.StartDate;
                    if (targetStartDate <= now)
                    {
                        normalizedRequest.Status = VoucherStatuses.Active;
                    }
                    else
                    {
                        normalizedRequest.Status = VoucherStatuses.Scheduled;
                    }

                    normalizedRequest.Reason = null;
                }

                // Nếu Admin cập nhật ngày tháng mà không chỉ định rõ trạng thái mới (hoặc trạng thái đang là Expired)
                if (normalizedRequest.Status == null || string.Equals(normalizedRequest.Status, VoucherStatuses.Expired, StringComparison.OrdinalIgnoreCase))
                {
                    var targetStartDate = normalizedRequest.StartDate ?? existingVoucher.StartDate;
                    var targetEndDate = normalizedRequest.EndDate ?? existingVoucher.EndDate;

                    // Nếu ngày kết thúc ở tương lai, tính toán lại trạng thái Active / Scheduled tương ứng
                    if (targetEndDate > now)
                    {
                        if (targetStartDate <= now)
                        {
                            if (string.Equals(oldStatus, VoucherStatuses.Scheduled, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(oldStatus, VoucherStatuses.Inactive, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(oldStatus, VoucherStatuses.Pending, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(oldStatus, VoucherStatuses.Expired, StringComparison.OrdinalIgnoreCase))
                            {
                                normalizedRequest.Status = VoucherStatuses.Active;
                            }
                        }
                        else
                        {
                            if (string.Equals(oldStatus, VoucherStatuses.Active, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(oldStatus, VoucherStatuses.Pending, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(oldStatus, VoucherStatuses.Expired, StringComparison.OrdinalIgnoreCase))
                            {
                                normalizedRequest.Status = VoucherStatuses.Scheduled;
                            }
                        }
                    }
                }
            }

            // ── Quy tắc 2: Giới hạn kích hoạt lại voucher đang tạm ngưng (Inactive) ──
            if (string.Equals(oldStatus, VoucherStatuses.Inactive, StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(normalizedRequest.Status, VoucherStatuses.Scheduled, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(normalizedRequest.Status, VoucherStatuses.Active, StringComparison.OrdinalIgnoreCase) ||
                 (string.Equals(_currentUserService.RoleName, "Staff", StringComparison.OrdinalIgnoreCase) &&
                  string.Equals(normalizedRequest.Status, VoucherStatuses.Pending, StringComparison.OrdinalIgnoreCase))))
            {
                var targetEndDate = normalizedRequest.EndDate ?? existingVoucher.EndDate;
                var targetTotalQuantity = normalizedRequest.TotalQuantity ?? existingVoucher.TotalQuantity;

                if (existingVoucher.IsDeleted)
                {
                    return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Cannot reactivate a deleted voucher.");
                }

                if (targetEndDate <= now)
                {
                    return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Cannot reactivate a voucher that has already expired. Please update the End Date to a future time.");
                }

                if (targetTotalQuantity.HasValue && existingVoucher.UsedQuantity >= targetTotalQuantity.Value)
                {
                    return Result<VoucherDto>.Failure("VALIDATION_ERROR", $"Cannot reactivate this voucher because it has reached its usage limit ({existingVoucher.UsedQuantity}/{targetTotalQuantity.Value}).");
                }
            }

            // Merge các trường cập nhật vào entity hiện tại qua AutoMapper
            _mapper.Map(normalizedRequest, existingVoucher);

            // Xử lý trường Reason: AutoMapper bỏ qua null nên cần gán trực tiếp nếu có logic xóa reason
            if (normalizedRequest.Reason == null &&
                ((string.Equals(_currentUserService.RoleName, "Staff", StringComparison.OrdinalIgnoreCase) && !string.Equals(normalizedRequest.Status, VoucherStatuses.Inactive, StringComparison.OrdinalIgnoreCase)) ||
                 (string.Equals(_currentUserService.RoleName, "Admin", StringComparison.OrdinalIgnoreCase) && string.Equals(normalizedRequest.Status, VoucherStatuses.Scheduled, StringComparison.OrdinalIgnoreCase))))
            {
                existingVoucher.Reason = null;
            }
            else if (normalizedRequest.Reason != null)
            {
                existingVoucher.Reason = normalizedRequest.Reason;
            }

            // Chuẩn hoá lại các trường chuỗi sau khi merge
            existingVoucher.VoucherCode = NormalizeCode(existingVoucher.VoucherCode);
            existingVoucher.DiscountType = NormalizeToken(existingVoucher.DiscountType);
            existingVoucher.DiscountTarget = NormalizeToken(existingVoucher.DiscountTarget);
            existingVoucher.Status = NormalizeStatus(existingVoucher.Status);
            existingVoucher.UpdatedAt = _timeProvider.UtcNow;

            // Validate toàn bộ dữ liệu tổng thể của entity sau khi merge thông qua CreateVoucherValidator (với cờ IsUpdate = true)
            var fullValidationRequest = MapToCreateDto(existingVoucher);
            var context = new ValidationContext<CreateVoucherDto>(fullValidationRequest);
            context.RootContextData["IsUpdate"] = true;
            var fullValidationResult = await _createValidator.ValidateAsync(context, cancellationToken);

            if (!fullValidationResult.IsValid)
            {
                return fullValidationResult.ToResult<VoucherDto>();
            }

            // Kiểm tra trùng mã voucher code (loại trừ chính voucher đang cập nhật)
            var voucherCodeExists = await _unitOfWork.Vouchers.ExistsVoucherCodeAsync(
                existingVoucher.VoucherCode,
                voucherId,
                cancellationToken);

            if (voucherCodeExists)
            {
                return Result<VoucherDto>.Conflict("Voucher code already exists.");
            }
        }

        // 5. Thực hiện lưu thay đổi trong Database Transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            _unitOfWork.Vouchers.Update(existingVoucher);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update voucher {VoucherId}", voucherId);
            throw;
        }

        // 6. Lấy dữ liệu mới nhất sau cập nhật và trả về VoucherDto
        var updatedVoucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId, cancellationToken) ?? existingVoucher;

        _logger.LogInformation(
            "Updated voucher {VoucherId} with code {VoucherCode}",
            voucherId,
            updatedVoucher.VoucherCode);

        return Result<VoucherDto>.Success(_mapper.Map<VoucherDto>(updatedVoucher));
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một voucher theo ID.
    /// </summary>
    /// <param name="voucherId">Mã định danh duy nhất (ID) của voucher.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa VoucherDto nếu tìm thấy, hoặc NotFound nếu không tồn tại.</returns>
    public async Task<Result<VoucherDto>> GetVoucherByIdAsync(
        int voucherId,
        CancellationToken cancellationToken = default)
    {
        // Kiểm tra ID hợp lệ
        if (voucherId <= 0)
        {
            return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Voucher ID must be greater than 0.");
        }

        // Tìm nạp voucher từ database
        var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId, cancellationToken);
        if (voucher is null)
        {
            return Result<VoucherDto>.NotFound("Voucher", voucherId);
        }

        return Result<VoucherDto>.Success(_mapper.Map<VoucherDto>(voucher));
    }



    // ── Các hàm tiện ích hỗ trợ chuẩn hóa dữ liệu (Normalize Helpers) ──────────────────

    /// <summary>
    /// Chuẩn hóa dữ liệu đầu vào cho request tạo mới voucher (cắt khoảng trắng, viết hoa các mã token/code).
    /// </summary>
    /// <param name="request">DTO tạo mới ban đầu.</param>
    /// <returns>DTO tạo mới đã được làm sạch và chuẩn hóa.</returns>
    private static CreateVoucherDto NormalizeCreateRequest(CreateVoucherDto request)
    {
        return new CreateVoucherDto
        {
            VoucherCode = NormalizeCode(request.VoucherCode),
            VoucherName = request.VoucherName.Trim(),
            VoucherDescription = request.VoucherDescription.Trim(),
            DiscountType = NormalizeToken(request.DiscountType),
            DiscountValue = request.DiscountValue,
            MaxDiscountCap = request.MaxDiscountCap,
            DiscountTarget = NormalizeToken(request.DiscountTarget),
            MinOrderAmount = request.MinOrderAmount,
            TotalQuantity = request.TotalQuantity,
            MaxUsagePerUser = request.MaxUsagePerUser,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = NormalizeStatus(request.Status)
        };
    }

    /// <summary>
    /// Chuẩn hóa dữ liệu đầu vào cho request cập nhật voucher từng phần (xử lý an toàn với các trường nullable).
    /// </summary>
    /// <param name="request">DTO cập nhật ban đầu.</param>
    /// <returns>DTO cập nhật đã được làm sạch và chuẩn hóa.</returns>
    private static UpdateVoucherDto NormalizeUpdateRequest(UpdateVoucherDto request)
    {
        return new UpdateVoucherDto
        {
            VoucherCode = request.VoucherCode is null ? null : NormalizeCode(request.VoucherCode),
            VoucherName = request.VoucherName?.Trim(),
            VoucherDescription = request.VoucherDescription?.Trim(),
            DiscountType = request.DiscountType is null ? null : NormalizeToken(request.DiscountType),
            DiscountValue = request.DiscountValue,
            MaxDiscountCap = request.MaxDiscountCap,
            DiscountTarget = request.DiscountTarget is null ? null : NormalizeToken(request.DiscountTarget),
            MinOrderAmount = request.MinOrderAmount,
            TotalQuantity = request.TotalQuantity,
            MaxUsagePerUser = request.MaxUsagePerUser,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status is null ? null : NormalizeStatus(request.Status),
            Reason = request.Reason?.Trim(),
            IsDeleted = request.IsDeleted
        };
    }

    /// <summary>
    /// Chuyển đổi một thực thể Voucher đã merge thành CreateVoucherDto để chạy bộ validation toàn diện (CreateVoucherValidator).
    /// </summary>
    /// <param name="voucher">Thực thể Voucher.</param>
    /// <returns>CreateVoucherDto chứa toàn bộ thông tin của voucher.</returns>
    private static CreateVoucherDto MapToCreateDto(Voucher voucher)
    {
        return new CreateVoucherDto
        {
            VoucherCode = voucher.VoucherCode,
            VoucherName = voucher.VoucherName,
            VoucherDescription = voucher.VoucherDescription,
            DiscountType = voucher.DiscountType,
            DiscountValue = voucher.DiscountValue,
            MaxDiscountCap = voucher.MaxDiscountCap,
            DiscountTarget = voucher.DiscountTarget,
            MinOrderAmount = voucher.MinOrderAmount,
            TotalQuantity = voucher.TotalQuantity,
            MaxUsagePerUser = voucher.MaxUsagePerUser,
            StartDate = voucher.StartDate,
            EndDate = voucher.EndDate,
            Status = voucher.Status
        };
    }

    /// <summary>
    /// Chuẩn hóa mã code của voucher (Cắt khoảng trắng và viết in hoa).
    /// </summary>
    private static string NormalizeCode(string value)
        => value.Trim().ToUpperInvariant();

    /// <summary>
    /// Chuẩn hóa mã định danh hằng số (Cắt khoảng trắng và viết in hoa, ví dụ: PERCENTAGE, FIXED, ORDER_TOTAL).
    /// </summary>
    private static string NormalizeToken(string value)
        => value.Trim().ToUpperInvariant();

    /// <summary>
    /// Chuẩn hóa chuỗi trạng thái voucher về đúng quy chuẩn viết hoa chữ cái đầu (PascalCase).
    /// </summary>
    private static string NormalizeStatus(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "scheduled" => "Scheduled",
            "active" => "Active",
            "inactive" => "Inactive",
            "expired" => "Expired",
            _ => value.Trim()
        };
}
