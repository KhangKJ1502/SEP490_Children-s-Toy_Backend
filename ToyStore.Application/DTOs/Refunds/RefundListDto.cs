using System;

namespace ToyStore.Application.DTOs.Refunds;

/// <summary>
/// Data Transfer Object (DTO) tóm tắt thông tin của yêu cầu hoàn tiền phục vụ hiển thị trên danh sách bảng (table) hoặc danh sách thẻ (card).
/// </summary>
public class RefundListDto
{
    /// <summary>
    /// Mã ID yêu cầu hoàn tiền.
    /// </summary>
    public int RefundId { get; set; }

    /// <summary>
    /// Mã ID đơn hàng gốc.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Mã code đơn hàng gốc (ví dụ: "ORD-20260301-1234").
    /// </summary>
    public string OrderCode { get; set; } = null!;

    /// <summary>
    /// Trạng thái đơn hàng gốc.
    /// </summary>
    public string OrderStatus { get; set; } = null!;

    /// <summary>
    /// Trạng thái thanh toán của đơn hàng.
    /// </summary>
    public string PaymentStatus { get; set; } = null!;

    /// <summary>
    /// Tên khách hàng.
    /// </summary>
    public string CustomerName { get; set; } = null!;

    /// <summary>
    /// Số điện thoại khách hàng.
    /// </summary>
    public string CustomerPhone { get; set; } = null!;

    /// <summary>
    /// Email của khách hàng.
    /// </summary>
    public string CustomerEmail { get; set; } = null!;

    /// <summary>
    /// Nội dung lý do hoàn tiền.
    /// </summary>
    public string? RefundReasonContent { get; set; }

    /// <summary>
    /// Tên người yêu cầu.
    /// </summary>
    public string? RequestedByName { get; set; }

    /// <summary>
    /// Số tiền phê duyệt ban đầu.
    /// </summary>
    public decimal ApprovedAmount { get; set; }

    /// <summary>
    /// Trạng thái yêu cầu hoàn tiền ("Pending", "Approved", "Returning", "Received", "Refunded", "Rejected", "Cancelled").
    /// </summary>
    public string RefundStatus { get; set; } = null!;

    /// <summary>
    /// Thời gian tạo yêu cầu (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Tên nhân viên CSKH/Staff được phân công.
    /// </summary>
    public string? AssignedToStaffName { get; set; }

    /// <summary>
    /// Tên nhân viên Kho/Merchandise được phân công nhận hàng.
    /// </summary>
    public string? AssignedToMerchName { get; set; }

    /// <summary>
    /// Nguồn tạo yêu cầu hoàn tiền ("Customer" hoặc "System").
    /// </summary>
    public string RefundSource { get; set; } = "Customer";

    /// <summary>
    /// Loại hình hoàn tiền ("ReturnAndRefund" hoặc "RefundOnly").
    /// </summary>
    public string RefundType { get; set; } = "ReturnAndRefund";

    /// <summary>
    /// Đánh dấu yêu cầu do hệ thống tự tạo khi GHN trả hàng thất bại (loại trừ RefundOnly).
    /// </summary>
    public bool IsSystemReturn => string.Equals(RefundSource, "System", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(RefundType, "RefundOnly", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Số tiền thực tế hoàn vào ví (Wallet) sau khi trừ chi phí (nếu có).
    /// </summary>
    public decimal FinalRefundAmount { get; set; }

    /// <summary>
    /// Đánh dấu khách đã thanh toán phí gửi lại hàng bị từ chối hay chưa.
    /// </summary>
    public bool ReturnToCustomerFeePaid { get; set; }

    /// <summary>
    /// Mức phí gửi lại hàng cho khách.
    /// </summary>
    public decimal ReturnToCustomerFee { get; set; }

    /// <summary>
    /// Phản hồi của khách hàng đối với hàng bị từ chối ("RECEIVE_BACK" hoặc "DISCARD").
    /// </summary>
    public string? CustomerResponse { get; set; }
}
