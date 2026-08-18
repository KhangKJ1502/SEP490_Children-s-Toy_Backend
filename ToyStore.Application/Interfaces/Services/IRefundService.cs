using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Interface định nghĩa toàn bộ hợp đồng dịch vụ xử lý nghiệp vụ Hoàn tiền / Đổi trả sản phẩm (Refund & Return).
/// </summary>
public interface IRefundService
{
    // ==========================================
    // Customer Endpoints
    // ==========================================

    /// <summary>
    /// Lấy danh sách các lý do hoàn tiền/trả hàng khả dụng từ cơ sở dữ liệu.
    /// </summary>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách các lý do hoàn tiền.</returns>
    Task<List<RefundReasonDto>> GetRefundReasonsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Khách hàng tạo yêu cầu hoàn tiền mới cho đơn hàng đã hoàn tất (Completed):
    /// - Kiểm tra quyền sở hữu đơn hàng và thời hạn đổi trả (trong vòng 7 ngày).
    /// - Xác thực danh sách sản phẩm hoàn, số lượng và lý do hoàn.
    /// - Tính toán số tiền hoàn dựa trên tỷ lệ khuyến mãi/voucher đã áp dụng trong đơn gốc.
    /// - Tự động phân công nhân viên phụ trách theo ca trực (Round-Robin).
    /// </summary>
    /// <param name="customerId">Mã ID của khách hàng.</param>
    /// <param name="dto">Dữ liệu yêu cầu hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin chi tiết yêu cầu hoàn tiền vừa tạo.</returns>
    Task<Result<RefundDto>> CreateRefundAsync(int customerId, CreateRefundDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách các yêu cầu hoàn tiền của khách hàng có phân trang và bộ lọc trạng thái.
    /// </summary>
    /// <param name="customerId">Mã ID của khách hàng.</param>
    /// <param name="filter">Bộ lọc danh sách.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách phân trang yêu cầu hoàn tiền.</returns>
    Task<PaginatedResponse<RefundListDto>> GetRefundsAsync(int customerId, RefundFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Khách hàng xem thông tin chi tiết một yêu cầu hoàn tiền của mình theo ID.
    /// </summary>
    /// <param name="customerId">Mã ID khách hàng.</param>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Chi tiết yêu cầu hoàn tiền.</returns>
    Task<Result<RefundDto>> GetRefundByIdAsync(int customerId, int refundId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Khách hàng hủy yêu cầu hoàn tiền khi còn ở trạng thái Pending.
    /// </summary>
    /// <param name="customerId">Mã ID khách hàng.</param>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền cần hủy.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin yêu cầu hoàn tiền sau khi cập nhật trạng thái hủy.</returns>
    Task<Result<RefundDto>> CancelRefundAsync(int customerId, int refundId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Khách hàng thanh toán phí hoàn hàng (khi trách nhiệm phí thuộc về khách hàng).
    /// </summary>
    /// <param name="customerId">Mã ID khách hàng.</param>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin yêu cầu hoàn tiền sau khi thanh toán phí.</returns>
    Task<Result<RefundDto>> PayReturnFeeAsync(int customerId, int refundId, CancellationToken cancellationToken = default);

    // ==========================================
    // Admin / Staff Endpoints
    // ==========================================

    /// <summary>
    /// Quản trị viên/Nhân viên lấy danh sách yêu cầu hoàn tiền theo phân quyền và bộ lọc.
    /// </summary>
    /// <param name="filter">Bộ lọc danh sách quản trị.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách phân trang yêu cầu hoàn tiền.</returns>
    Task<PaginatedResponse<RefundListDto>> GetAdminRefundsAsync(AdminRefundFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quản trị viên/Nhân viên xem thông tin chi tiết một yêu cầu hoàn tiền trong trang quản trị.
    /// </summary>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="currentUserId">Mã ID người dùng hiện tại.</param>
    /// <param name="currentUserRoleId">Mã ID vai trò người dùng hiện tại.</param>
    /// <param name="isAdmin">Cờ đánh dấu có phải Admin hay không.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Chi tiết yêu cầu hoàn tiền.</returns>
    Task<Result<RefundDto>> AdminGetRefundByIdAsync(int refundId, int currentUserId, byte currentUserRoleId, bool isAdmin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật trạng thái tiến trình xử lý yêu cầu hoàn tiền:
    /// - Kiểm tra quyền chuyển trạng thái theo ma trận chuyển trạng thái hợp lệ.
    /// - Khi duyệt chuyển sang Returned/Received -> cập nhật hoàn tiền vào số dư Ví (Wallet) của khách hàng.
    /// - Cập nhật kho hàng (tăng lại tồn kho sản phẩm nếu hoàn thành công).
    /// - Gửi thông báo tới khách hàng.
    /// </summary>
    /// <param name="staffId">Mã ID nhân viên thực hiện.</param>
    /// <param name="roleId">Mã ID vai trò của nhân viên.</param>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="dto">Dữ liệu cập nhật trạng thái.</param>
    /// <param name="isAdmin">Cờ đánh dấu có phải Admin hay không.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin yêu cầu hoàn tiền sau khi cập nhật.</returns>
    Task<Result<RefundDto>> UpdateRefundStatusAsync(int staffId, byte roleId, int refundId, UpdateRefundStatusDto dto, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phân công lại nhân viên phụ trách yêu cầu hoàn tiền.
    /// </summary>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="dto">Thông tin nhân viên mới và lý do chuyển giao.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Kết quả thực hiện.</returns>
    Task<Result> ReassignRefundAsync(int refundId, ToyStore.Application.DTOs.Assignments.ReassignOrderRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hệ thống tự động tạo yêu cầu hoàn tiền khi đơn hàng giao thất bại và đơn vị vận chuyển GHN trả hàng về kho.
    /// </summary>
    /// <param name="order">Thực thể đơn hàng.</param>
    /// <param name="refundReasonId">Mã ID lý do hoàn tiền.</param>
    /// <param name="initialStatusId">Mã ID trạng thái khởi tạo (tùy chọn).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể OrderRefund vừa được hệ thống tạo (hoặc null nếu không cần tạo).</returns>
    Task<OrderRefund?> CreateSystemRefundForDeliveryFailAsync(
        Order order, byte refundReasonId, byte? initialStatusId = null, CancellationToken cancellationToken = default);
}
