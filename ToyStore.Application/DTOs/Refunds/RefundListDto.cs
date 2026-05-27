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
}
