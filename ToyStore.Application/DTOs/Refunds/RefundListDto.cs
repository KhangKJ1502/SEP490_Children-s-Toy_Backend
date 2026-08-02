using System;

namespace ToyStore.Application.DTOs.Refunds;

public class RefundListDto
{
    public int RefundId { get; set; }
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = null!;
    public string OrderStatus { get; set; } = null!;
    public string PaymentStatus { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public string? RefundReasonContent { get; set; }
    public string? RequestedByName { get; set; }
    public decimal ApprovedAmount { get; set; }
    public string RefundStatus { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? AssignedToStaffName { get; set; }
    public string? AssignedToMerchName { get; set; }

    /// <summary>"Customer" hoặc "System" — phân biệt nguồn tạo refund.</summary>
    public string RefundSource { get; set; } = "Customer";

    /// <summary>True nếu refund do hệ thống tự tạo (GHN returned). UI dùng để ẩn nút pickup và hiển thị label "System return".</summary>
    public bool IsSystemReturn => string.Equals(RefundSource, "System", StringComparison.OrdinalIgnoreCase);

    /// <summary>Số tiền thực tế credit vào ví. 0 nếu chưa Approve (DB default). Dùng cho Customer FE card display.</summary>
    public decimal FinalRefundAmount { get; set; }

    public bool ReturnToCustomerFeePaid { get; set; }
    public decimal ReturnToCustomerFee { get; set; }
    public string? CustomerResponse { get; set; }
}
