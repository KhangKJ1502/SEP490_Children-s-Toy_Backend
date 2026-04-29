using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Vouchers;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service xử lý nghiệp vụ voucher.
/// </summary>
public interface IVoucherService
{
    /// <summary>
    /// Lấy danh sách voucher có phân trang, sắp xếp và tìm kiếm.
    /// </summary>
    Task<Result<PaginatedResponse<VoucherListDto>>> GetVouchersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới voucher.
    /// </summary>
    Task<Result<VoucherDto>> CreateVoucherAsync(
        CreateVoucherDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật voucher.
    /// </summary>
    Task<Result<VoucherDto>> UpdateVoucherAsync(
        int voucherId,
        UpdateVoucherDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chi tiết voucher theo ID.
    /// </summary>
    Task<Result<VoucherDto>> GetVoucherByIdAsync(
        int voucherId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xoá mềm voucher theo ID.
    /// </summary>
    Task<Result> DeleteVoucherAsync(
        int voucherId,
        CancellationToken cancellationToken = default);
}
