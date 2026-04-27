using ToyStore.Application.Common.Models.Vouchers;
using ToyStore.Application.DTOs;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository xử lý truy vấn dữ liệu voucher.
/// </summary>
public interface IVoucherRepository
{
    /// <summary>
    /// Lấy danh sách voucher có phân trang và tìm kiếm.
    /// </summary>
    Task<PaginatedResponse<VoucherModel>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy voucher theo ID.
    /// </summary>
    Task<VoucherModel?> GetByIdAsync(int voucherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy voucher theo code.
    /// </summary>
    Task<VoucherModel?> GetByCodeAsync(string voucherCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra code voucher đã tồn tại hay chưa.
    /// </summary>
    Task<bool> ExistsVoucherCodeAsync(
        string voucherCode,
        int? excludeVoucherId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm voucher mới vào context.
    /// </summary>
    Task AddAsync(VoucherModel voucher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật voucher vào context.
    /// </summary>
    void Update(VoucherModel voucher);
}
