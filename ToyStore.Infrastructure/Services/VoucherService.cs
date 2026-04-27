using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.Common.Models.Vouchers;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Vouchers;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Service xử lý nghiệp vụ voucher.
/// </summary>
public class VoucherService : IVoucherService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateVoucherDto> _createValidator;
    private readonly IValidator<UpdateVoucherDto> _updateValidator;
    private readonly ILogger<VoucherService> _logger;

    public VoucherService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<CreateVoucherDto> createValidator,
        IValidator<UpdateVoucherDto> updateValidator,
        ILogger<VoucherService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<PaginatedResponse<VoucherListDto>> GetVouchersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);

        var pagedVouchers = await _unitOfWork.Vouchers.GetPagedAsync(
            normalizedPageNumber,
            normalizedPageSize,
            sortBy,
            sortDesc,
            searchTerm,
            status,
            cancellationToken);

        var items = _mapper.Map<List<VoucherListDto>>(pagedVouchers.Items);

        _logger.LogInformation(
            "Retrieved vouchers list with PageNumber {PageNumber}, PageSize {PageSize}, SearchTerm {SearchTerm}, Status {Status}",
            normalizedPageNumber,
            normalizedPageSize,
            searchTerm,
            status);

        return new PaginatedResponse<VoucherListDto>(
            items,
            pagedVouchers.TotalCount,
            normalizedPageNumber,
            normalizedPageSize);
    }

    public async Task<Result<VoucherDto>> CreateVoucherAsync(
        CreateVoucherDto request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeCreateRequest(request);
        var validationResult = await _createValidator.ValidateAsync(normalizedRequest, cancellationToken);

        if (!validationResult.IsValid)
        {
            return validationResult.ToResult<VoucherDto>();
        }

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

        await _unitOfWork.Vouchers.AddAsync(voucherModel, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        var normalizedRequest = NormalizeUpdateRequest(request);
        var updateValidation = await _updateValidator.ValidateAsync(normalizedRequest, cancellationToken);

        if (!updateValidation.IsValid)
        {
            return updateValidation.ToResult<VoucherDto>();
        }

        var existingVoucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId, cancellationToken);
        if (existingVoucher is null)
        {
            return Result<VoucherDto>.Failure("NOT_FOUND", "Voucher not found.");
        }

        var mergedVoucher = CloneVoucher(existingVoucher);
        _mapper.Map(normalizedRequest, mergedVoucher);

        // Chuẩn hoá lại các field string trước khi validate toàn bộ dữ liệu.
        mergedVoucher.VoucherCode = NormalizeCode(mergedVoucher.VoucherCode);
        mergedVoucher.DiscountType = NormalizeToken(mergedVoucher.DiscountType);
        mergedVoucher.DiscountTarget = NormalizeToken(mergedVoucher.DiscountTarget);
        mergedVoucher.Status = NormalizeStatus(mergedVoucher.Status);
        mergedVoucher.UpdatedAt = DateTime.UtcNow;

        var fullValidationRequest = MapToCreateRequest(mergedVoucher);
        var fullValidationResult = await _createValidator.ValidateAsync(fullValidationRequest, cancellationToken);

        if (!fullValidationResult.IsValid)
        {
            return fullValidationResult.ToResult<VoucherDto>();
        }

        var voucherCodeExists = await _unitOfWork.Vouchers.ExistsVoucherCodeAsync(
            mergedVoucher.VoucherCode,
            voucherId,
            cancellationToken);

        if (voucherCodeExists)
        {
            return Result<VoucherDto>.Conflict("Voucher code already exists.");
        }

        _unitOfWork.Vouchers.Update(mergedVoucher);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedVoucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId, cancellationToken) ?? mergedVoucher;

        _logger.LogInformation(
            "Updated voucher {VoucherId} with code {VoucherCode}",
            voucherId,
            updatedVoucher.VoucherCode);

        return Result<VoucherDto>.Success(_mapper.Map<VoucherDto>(updatedVoucher));
    }

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

    private static VoucherModel CloneVoucher(VoucherModel source)
    {
        return new VoucherModel
        {
            VoucherId = source.VoucherId,
            CreatedBy = source.CreatedBy,
            VoucherCode = source.VoucherCode,
            VoucherName = source.VoucherName,
            VoucherDescription = source.VoucherDescription,
            DiscountType = source.DiscountType,
            DiscountValue = source.DiscountValue,
            MaxDiscountCap = source.MaxDiscountCap,
            DiscountTarget = source.DiscountTarget,
            MinOrderAmount = source.MinOrderAmount,
            TotalQuantity = source.TotalQuantity,
            UsedQuantity = source.UsedQuantity,
            MaxUsagePerUser = source.MaxUsagePerUser,
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            Status = source.Status,
            IsDeleted = source.IsDeleted,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt
        };
    }

    private static CreateVoucherDto MapToCreateRequest(VoucherModel voucher)
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
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeToken(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeStatus(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "scheduled" => "Scheduled",
            "active" => "Active",
            "inactive" => "Inactive",
            "expired" => "Expired",
            _ => value.Trim()
        };
    }
}
