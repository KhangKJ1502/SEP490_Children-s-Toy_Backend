using AutoMapper;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Vouchers;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators.Vouchers;
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
    private readonly CreateVoucherValidator _createValidator;
    private readonly UpdateVoucherValidator _updateValidator;

    public VoucherService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<VoucherService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _createValidator = new CreateVoucherValidator();
        _updateValidator = new UpdateVoucherValidator();
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

        var voucherModel = _mapper.Map<VoucherModel>(normalizedRequest);
        voucherModel.CreatedBy = null;
        voucherModel.CreatedAt = DateTime.UtcNow;
        voucherModel.UpdatedAt = null;
        voucherModel.UsedQuantity = 0;
        voucherModel.IsDeleted = false;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Vouchers.AddAsync(voucherModel, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create voucher with code {VoucherCode}", voucherModel.VoucherCode);
            throw;
        }

        var createdVoucher = await _unitOfWork.Vouchers.GetByCodeAsync(voucherModel.VoucherCode, cancellationToken);
        if (createdVoucher is null)
        {
            _logger.LogError("Failed to load created voucher by code {VoucherCode}", voucherModel.VoucherCode);
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

        // Merge partial update vào entity hiện tại, AutoMapper chỉ ghi đè field != null
        _mapper.Map(normalizedRequest, existingVoucher);

        // Chuẩn hoá lại các field string sau khi merge
        existingVoucher.VoucherCode = NormalizeCode(existingVoucher.VoucherCode);
        existingVoucher.DiscountType = NormalizeToken(existingVoucher.DiscountType);
        existingVoucher.DiscountTarget = NormalizeToken(existingVoucher.DiscountTarget);
        existingVoucher.Status = NormalizeStatus(existingVoucher.Status);
        existingVoucher.UpdatedAt = DateTime.UtcNow;

        // Validate toàn bộ dữ liệu sau merge
        var fullValidationRequest = MapToCreateDto(existingVoucher);
        var fullValidationResult = await _createValidator.ValidateAsync(fullValidationRequest, cancellationToken);

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

        var updatedVoucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId, cancellationToken) ?? existingVoucher;

        _logger.LogInformation(
            "Updated voucher {VoucherId} with code {VoucherCode}",
            voucherId,
            updatedVoucher.VoucherCode);

        return Result<VoucherDto>.Success(_mapper.Map<VoucherDto>(updatedVoucher));
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
            Status = request.Status is null ? null : NormalizeStatus(request.Status)
        };
    }

    // Map VoucherModel → CreateVoucherDto để validate toàn bộ sau merge
    private static CreateVoucherDto MapToCreateDto(VoucherModel voucher)
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
