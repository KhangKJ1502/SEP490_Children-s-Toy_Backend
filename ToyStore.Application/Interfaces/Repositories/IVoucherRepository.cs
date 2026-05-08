using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository xử lý truy vấn dữ liệu voucher.
/// </summary>
public interface IVoucherRepository
{
    /// <summary>
    /// Lấy danh sách voucher có phân trang và tìm kiếm.
    /// </summary>
    Task<PaginatedResponse<Voucher>> GetPagedAsync(
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
    Task<Voucher?> GetByIdAsync(int voucherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy voucher theo code.
    /// </summary>
    Task<Voucher?> GetByCodeAsync(string voucherCode, CancellationToken cancellationToken = default);

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
    Task AddAsync(Voucher voucher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật voucher vào context.
    /// </summary>
    void Update(Voucher voucher);

    Task<int> CountUsageByAccountAsync(int voucherId, int accountId, CancellationToken cancellationToken = default);
}
