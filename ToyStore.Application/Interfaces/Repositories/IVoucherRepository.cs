using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Interface định nghĩa các phương thức thao tác dữ liệu (CRUD, truy vấn phân trang, kiểm tra trùng lặp) cho thực thể Voucher.
/// </summary>
public interface IVoucherRepository
{
    /// <summary>
    /// Lấy danh sách voucher từ cơ sở dữ liệu có phân trang, sắp xếp và lọc theo từ khóa, trạng thái (bỏ qua các bản ghi đã xóa mềm).
    /// </summary>
    /// <param name="pageNumber">Số trang cần lấy (bắt đầu từ 1).</param>
    /// <param name="pageSize">Số lượng bản ghi mỗi trang.</param>
    /// <param name="sortBy">Tên trường sắp xếp.</param>
    /// <param name="sortDesc">true để sắp xếp giảm dần, false để tăng dần.</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm theo VoucherCode, VoucherName, VoucherDescription.</param>
    /// <param name="status">Trạng thái voucher cần lọc.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng PaginatedResponse chứa danh sách thực thể Voucher và tổng số bản ghi.</returns>
    Task<PaginatedResponse<Voucher>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thực thể voucher theo khóa chính (VoucherId), bỏ qua các bản ghi đã xóa mềm (IsDeleted = true).
    /// Có theo dõi (tracked) để phục vụ cập nhật.
    /// </summary>
    /// <param name="voucherId">Mã ID của voucher.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể Voucher nếu tìm thấy, hoặc null nếu không tồn tại hoặc đã bị xóa.</returns>
    Task<Voucher?> GetByIdAsync(int voucherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thực thể voucher theo mã voucher (VoucherCode), không theo dõi (AsNoTracking), bỏ qua bản ghi đã xóa mềm.
    /// </summary>
    /// <param name="voucherCode">Mã code của voucher (ví dụ: DISCOUNT10K).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể Voucher nếu tìm thấy, hoặc null nếu không tồn tại.</returns>
    Task<Voucher?> GetByCodeAsync(string voucherCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra xem mã voucher đã tồn tại trong cơ sở dữ liệu hay chưa (không phân biệt hoa thường, bỏ qua các bản ghi đã xóa mềm).
    /// Có thể loại trừ một voucherId cụ thể khi thực hiện cập nhật.
    /// </summary>
    /// <param name="voucherCode">Mã code cần kiểm tra trùng lặp.</param>
    /// <param name="excludeVoucherId">Mã ID voucher cần loại trừ (thường dùng khi cập nhật chính nó).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>true nếu mã đã tồn tại, false nếu chưa tồn tại.</returns>
    Task<bool> ExistsVoucherCodeAsync(
        string voucherCode,
        int? excludeVoucherId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm một thực thể voucher mới vào DbContext (chưa lưu xuống cơ sở dữ liệu cho tới khi gọi SaveChangesAsync).
    /// </summary>
    /// <param name="voucher">Thực thể Voucher cần thêm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    Task AddAsync(Voucher voucher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh dấu thực thể voucher là đã chỉnh sửa trong DbContext.
    /// </summary>
    /// <param name="voucher">Thực thể Voucher cần cập nhật.</param>
    void Update(Voucher voucher);

    /// <summary>
    /// Đếm số lần một tài khoản người dùng đã sử dụng một voucher cụ thể.
    /// Loại trừ các đơn hàng SE_PAY đang ở trạng thái PENDING chưa thanh toán thành công.
    /// </summary>
    /// <param name="voucherId">Mã ID của voucher.</param>
    /// <param name="accountId">Mã ID của tài khoản khách hàng.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Số lần tài khoản đã sử dụng voucher này.</returns>
    Task<int> CountUsageByAccountAsync(int voucherId, int accountId, CancellationToken cancellationToken = default);
}
