using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Vouchers;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators.Vouchers;
using ToyStore.Application.Common.Models;
using Microsoft.Extensions.Options;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Service xử lý nghiệp vụ voucher.
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
        _unitOfWork         = unitOfWork;
        _mapper             = mapper;
        _logger             = logger;
        _createValidator    = createValidator;
        _updateValidator    = updateValidator;
        _timeProvider       = timeProvider;
        _currentUserService = currentUserService;
        _thresholds         = thresholds.Value;
    }

    public async Task<Result<PaginatedResponse<VoucherListDto>>> GetVouchersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
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

        var pagedVouchers = await _unitOfWork.Vouchers.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            status,
            cancellationToken);

        var items = _mapper.Map<List<VoucherListDto>>(pagedVouchers.Items);

        // Populate CurrentUserUsageCount if user is authenticated
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

        var response = new PaginatedResponse<VoucherListDto>(
            items,
            pagedVouchers.TotalCount,
            pageNumber,
            pageSize);

        return Result<PaginatedResponse<VoucherListDto>>.Success(response);
    }

    public async Task<Result<VoucherDto>> CreateVoucherAsync(
        CreateVoucherDto request,
        CancellationToken cancellationToken = default)
    {
        // Chuẩn hoá input trước khi validate
        var normalizedRequest = NormalizeCreateRequest(request);
        var validationResult = await _createValidator.ValidateAsync(normalizedRequest, cancellationToken);

        if (!validationResult.IsValid)
        {
            return validationResult.ToResult<VoucherDto>();
        }

        // Kiểm tra trùng voucher code
        var voucherCodeExists = await _unitOfWork.Vouchers.ExistsVoucherCodeAsync(
            normalizedRequest.VoucherCode,
            null,
            cancellationToken);

        if (voucherCodeExists)
        {
            return Result<VoucherDto>.Conflict("Voucher code already exists.");
        }

        var voucher = _mapper.Map<Voucher>(normalizedRequest);
        voucher.CreatedBy = _currentUserService.AccountId;
        voucher.CreatedAt = _timeProvider.UtcNow;
        voucher.UpdatedAt = null;
        voucher.UsedQuantity = 0;
        voucher.IsDeleted = false;

        // Apply Status Transition Rules
        if (string.Equals(_currentUserService.RoleName, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(voucher.DiscountTarget, "FINAL_PRICE", StringComparison.OrdinalIgnoreCase))
            {
                voucher.Status = VoucherStatuses.Pending;
            }
            else if (string.Equals(voucher.DiscountType, "PERCENTAGE", StringComparison.OrdinalIgnoreCase))
            {
                if (voucher.MaxDiscountCap > _thresholds.MaxDiscountCap || 
                   (voucher.MaxDiscountCap * (voucher.TotalQuantity ?? 1)) > _thresholds.MaxTotalDiscount)
                {
                    voucher.Status = VoucherStatuses.Pending;
                }
                else
                {
                    voucher.Status = VoucherStatuses.Scheduled;
                }
            }
            else if (string.Equals(voucher.DiscountType, "FIXED", StringComparison.OrdinalIgnoreCase))
            {
                if (voucher.DiscountValue > _thresholds.MaxDiscountCap || 
                   (voucher.DiscountValue * (voucher.TotalQuantity ?? 1)) > _thresholds.MaxTotalDiscount)
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
        else // Admin
        {
            voucher.Status = VoucherStatuses.Scheduled;
        }

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

    public async Task<Result<VoucherDto>> UpdateVoucherAsync(
        int voucherId,
        UpdateVoucherDto request,
        CancellationToken cancellationToken = default)
    {
        if (voucherId <= 0)
        {
            return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Voucher ID must be greater than 0.");
        }

        // Validate partial update request
        var normalizedRequest = NormalizeUpdateRequest(request);
        var updateValidation = await _updateValidator.ValidateAsync(normalizedRequest, cancellationToken);

        if (!updateValidation.IsValid)
        {
            return updateValidation.ToResult<VoucherDto>();
        }

        // Lấy voucher hiện tại
        var existingVoucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId, cancellationToken);
        if (existingVoucher is null)
        {
            return Result<VoucherDto>.NotFound("Voucher", voucherId);
        }

        // Nếu là yêu cầu xoá (Soft Delete)
        if (normalizedRequest.IsDeleted == true)
        {
            if (string.Equals(existingVoucher.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Cannot delete an Active voucher.");
            }

            existingVoucher.IsDeleted = true;
            existingVoucher.UpdatedAt = _timeProvider.UtcNow;

            // Fix legacy invalid data to satisfy SQL Server CHECK constraints during soft delete
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

            // Role-based logic before merge
            if (string.Equals(_currentUserService.RoleName, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                // Staff cannot explicitly set to Scheduled or Approved
                if (string.Equals(normalizedRequest.Status, VoucherStatuses.Scheduled, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(normalizedRequest.Status, VoucherStatuses.Active, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedRequest.Status = VoucherStatuses.Pending;
                }

                // If updating a Rejected voucher, it goes back to Pending automatically (unless emergency Inactive)
                if (string.Equals(oldStatus, VoucherStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(normalizedRequest.Status, VoucherStatuses.Inactive, StringComparison.OrdinalIgnoreCase))
                    {
                        normalizedRequest.Status = VoucherStatuses.Pending;
                        normalizedRequest.Reason = null; // clear admin reason
                    }
                }
                
                // Staff cannot update Reason
                normalizedRequest.Reason = null;
            }
            else if (string.Equals(_currentUserService.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Admin Rejecting must provide Reason
                if (string.Equals(normalizedRequest.Status, VoucherStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(normalizedRequest.Reason) && string.IsNullOrWhiteSpace(existingVoucher.Reason))
                    {
                        return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Reason is required when rejecting a voucher.");
                    }
                }
                
                // Admin Approving clears Reason and calculates dynamic status
                if (string.Equals(normalizedRequest.Status, VoucherStatuses.Scheduled, StringComparison.OrdinalIgnoreCase))
                {
                    var now = _timeProvider.UtcNow;
                    
                    if (existingVoucher.EndDate <= now)
                    {
                        return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Cannot approve a voucher that has already expired. Please reject it or ask staff to update the dates.");
                    }
                    
                    if (existingVoucher.StartDate <= now)
                    {
                        normalizedRequest.Status = VoucherStatuses.Active;
                    }
                    else
                    {
                        normalizedRequest.Status = VoucherStatuses.Scheduled;
                    }
                    
                    normalizedRequest.Reason = null;
                }
            }

            if (normalizedRequest.StartDate.HasValue && normalizedRequest.StartDate.Value != existingVoucher.StartDate)
            {
                if (normalizedRequest.StartDate.Value < _timeProvider.UtcNow.AddMinutes(9))
                {
                    return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Start date must be at least 10 minutes from now.");
                }
            }

            // Merge partial update vào entity hiện tại, AutoMapper đã được cấu hình PreCondition để an toàn với nullable value types
            _mapper.Map(normalizedRequest, existingVoucher);

            // Xử lý field Reason do AutoMapper có thể không set null được nếu property src null.
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

            // Chuẩn hoá lại các field string sau khi merge
            existingVoucher.VoucherCode = NormalizeCode(existingVoucher.VoucherCode);
            existingVoucher.DiscountType = NormalizeToken(existingVoucher.DiscountType);
            existingVoucher.DiscountTarget = NormalizeToken(existingVoucher.DiscountTarget);
            existingVoucher.Status = NormalizeStatus(existingVoucher.Status);
            existingVoucher.UpdatedAt = _timeProvider.UtcNow;

            // Validate toàn bộ dữ liệu sau merge
            var fullValidationRequest = MapToCreateDto(existingVoucher);
            var context = new ValidationContext<CreateVoucherDto>(fullValidationRequest);
            context.RootContextData["IsUpdate"] = true;
            var fullValidationResult = await _createValidator.ValidateAsync(context, cancellationToken);

            if (!fullValidationResult.IsValid)
            {
                return fullValidationResult.ToResult<VoucherDto>();
            }

            // Kiểm tra trùng voucher code (bỏ qua chính mình)
            var voucherCodeExists = await _unitOfWork.Vouchers.ExistsVoucherCodeAsync(
                existingVoucher.VoucherCode,
                voucherId,
                cancellationToken);

            if (voucherCodeExists)
            {
                return Result<VoucherDto>.Conflict("Voucher code already exists.");
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update voucher {VoucherId}", voucherId);
            throw;
        }

        var updatedVoucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId, cancellationToken) ?? existingVoucher;

        _logger.LogInformation(
            "Updated voucher {VoucherId} with code {VoucherCode}",
            voucherId,
            updatedVoucher.VoucherCode);

        return Result<VoucherDto>.Success(_mapper.Map<VoucherDto>(updatedVoucher));
    }

    public async Task<Result<VoucherDto>> GetVoucherByIdAsync(
        int voucherId,
        CancellationToken cancellationToken = default)
    {
        if (voucherId <= 0)
        {
            return Result<VoucherDto>.Failure("VALIDATION_ERROR", "Voucher ID must be greater than 0.");
        }

        var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId, cancellationToken);
        if (voucher is null)
        {
            return Result<VoucherDto>.NotFound("Voucher", voucherId);
        }

        return Result<VoucherDto>.Success(_mapper.Map<VoucherDto>(voucher));
    }



    // ── Normalize helpers ─────────────────────────────────────────────────────

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

    // Map Voucher → CreateVoucherDto để validate toàn bộ sau merge
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

    private static string NormalizeCode(string value)
        => value.Trim().ToUpperInvariant();

    private static string NormalizeToken(string value)
        => value.Trim().ToUpperInvariant();

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
