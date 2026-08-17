using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Interface định nghĩa các phương thức thao tác dữ liệu (CRUD, truy vấn phân trang, tìm kiếm) cho thực thể Yêu cầu hoàn tiền (OrderRefund).
/// </summary>
public interface IRefundRepository
{
    /// <summary>
    /// Lấy danh sách các lý do hoàn tiền đang hoạt động (IsActive = true).
    /// </summary>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách thực thể OrderRefundReason.</returns>
    Task<List<OrderRefundReason>> GetActiveReasonsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm lý do hoàn tiền theo nội dung mô tả lý do.
    /// </summary>
    /// <param name="content">Nội dung lý do hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể OrderRefundReason nếu tìm thấy, ngược lại null.</returns>
    Task<OrderRefundReason?> GetReasonByContentAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách yêu cầu hoàn tiền của khách hàng theo bộ lọc và phân trang.
    /// </summary>
    /// <param name="customerId">Mã ID khách hàng.</param>
    /// <param name="filter">Bộ lọc danh sách yêu cầu hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách phân trang PaginatedResponse chứa RefundListDto.</returns>
    Task<PaginatedResponse<RefundListDto>> GetRefundsAsync(int customerId, RefundFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách yêu cầu hoàn tiền phía quản trị viên/nhân viên có phân trang và bộ lọc nâng cao.
    /// </summary>
    /// <param name="filter">Bộ lọc danh sách quản trị.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách phân trang PaginatedResponse chứa RefundListDto.</returns>
    Task<PaginatedResponse<RefundListDto>> GetAdminRefundsAsync(AdminRefundFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin chi tiết một yêu cầu hoàn tiền theo mã ID (kèm đầy đủ Order, Status, Reason, Details, Images, Histories, Assignments).
    /// </summary>
    /// <param name="id">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể OrderRefund nếu tìm thấy, ngược lại null.</returns>
    Task<OrderRefund?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm yêu cầu hoàn tiền gần nhất theo mã ID đơn hàng gốc.
    /// </summary>
    /// <param name="orderId">Mã ID đơn hàng.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể OrderRefund nếu tìm thấy, ngược lại null.</returns>
    Task<OrderRefund?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm yêu cầu hoàn tiền theo mã vận đơn chuyển hàng ban đầu từ đơn vị vận chuyển GHN.
    /// </summary>
    /// <param name="code">Mã vận đơn giao hàng.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể OrderRefund nếu tìm thấy, ngược lại null.</returns>
    Task<OrderRefund?> GetByShippingOrderCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm yêu cầu hoàn tiền theo mã vận đơn chuyển tiếp hoặc mã vận đơn trả hàng (ReturnOrderCode).
    /// </summary>
    /// <param name="code">Mã vận đơn.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể OrderRefund nếu tìm thấy, ngược lại null.</returns>
    Task<OrderRefund?> GetByShippingOrReturnOrderCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm mới một yêu cầu hoàn tiền vào DbContext.
    /// </summary>
    /// <param name="refund">Thực thể OrderRefund cần thêm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể OrderRefund sau khi thêm.</returns>
    Task<OrderRefund> AddAsync(OrderRefund refund, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh dấu thực thể OrderRefund là Modified để cập nhật thay đổi.
    /// </summary>
    /// <param name="refund">Thực thể OrderRefund cần cập nhật.</param>
    void Update(OrderRefund refund);

    /// <summary>
    /// Lấy danh sách các yêu cầu hoàn tiền đang quá hạn thanh toán phí hoàn trả (Stale Unpaid Refunds) để Background Worker tự động hủy.
    /// </summary>
    /// <param name="cutoff">Thời điểm mốc hết hạn.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách các yêu cầu hoàn tiền quá hạn.</returns>
    Task<List<OrderRefund>> GetStaleUnpaidRefundsAsync(System.DateTime cutoff, CancellationToken cancellationToken = default);
}
