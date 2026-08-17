using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Vouchers;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Interface định nghĩa các phương thức xử lý nghiệp vụ cho tính năng Voucher (mã khuyến mãi/giảm giá).
/// </summary>
public interface IVoucherService
{
    /// <summary>
    /// Lấy danh sách voucher có phân trang, sắp xếp và lọc theo từ khóa, trạng thái.
    /// Tự động bổ sung số lần đã sử dụng của tài khoản người dùng hiện tại nếu đã đăng nhập.
    /// </summary>
    /// <param name="pageNumber">Số trang hiện tại (bắt đầu từ 1).</param>
    /// <param name="pageSize">Kích thước trang (số lượng voucher trên mỗi trang, từ 1 đến 100).</param>
    /// <param name="sortBy">Tên trường cần sắp xếp (ví dụ: VoucherCode, DiscountValue, StartDate, EndDate, Status, CreatedAt).</param>
    /// <param name="sortDesc">Thứ tự sắp xếp: true để giảm dần, false để tăng dần.</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm (mã, tên hoặc mô tả của voucher).</param>
    /// <param name="status">Trạng thái voucher cần lọc (Scheduled, Active, Inactive, Expired, Pending, Rejected).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa PaginatedResponse danh sách VoucherListDto.</returns>
    Task<Result<PaginatedResponse<VoucherListDto>>> GetVouchersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới một voucher với đầy đủ kiểm tra tính hợp lệ và phân luồng trạng thái theo phân quyền (Admin duyệt trực tiếp, Staff gửi duyệt nếu vượt ngưỡng rủi ro).
    /// </summary>
    /// <param name="request">DTO chứa các trường thông tin cần thiết để tạo mới voucher.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa VoucherDto chi tiết của voucher vừa tạo thành công, hoặc lỗi nếu không hợp lệ/trùng mã.</returns>
    Task<Result<VoucherDto>> CreateVoucherAsync(
        CreateVoucherDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật thông tin voucher theo ID, hỗ trợ cập nhật từng phần (Partial Update).
    /// Áp dụng các quy tắc bảo vệ: không chỉnh sửa trường tài chính nếu voucher đã sử dụng, không sửa voucher đã hết hạn (ngoại trừ gia hạn/ngừng hoạt động),
    /// và quy tắc phân quyền phê duyệt (Staff sửa voucher Active/Scheduled sẽ chuyển về Pending).
    /// </summary>
    /// <param name="voucherId">Mã định danh duy nhất (ID) của voucher cần cập nhật.</param>
    /// <param name="request">DTO chứa các trường cần cập nhật hoặc yêu cầu xóa mềm (IsDeleted = true).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa VoucherDto chi tiết sau khi cập nhật thành công, hoặc lỗi nếu vi phạm ràng buộc nghiệp vụ.</returns>
    Task<Result<VoucherDto>> UpdateVoucherAsync(
        int voucherId,
        UpdateVoucherDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin chi tiết một voucher theo ID.
    /// </summary>
    /// <param name="voucherId">Mã định danh duy nhất (ID) của voucher.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa VoucherDto chi tiết nếu tìm thấy, hoặc NotFound nếu không tồn tại.</returns>
    Task<Result<VoucherDto>> GetVoucherByIdAsync(
        int voucherId,
        CancellationToken cancellationToken = default);
}
