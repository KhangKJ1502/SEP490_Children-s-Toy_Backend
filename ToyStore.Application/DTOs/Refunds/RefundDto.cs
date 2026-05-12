using System;
using System.Collections.Generic;

namespace ToyStore.Application.DTOs.Refunds;

public class RefundDto
{
    public int RefundId { get; set; }
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = null!;
    public string OrderStatus { get; set; } = null!;
    public string PaymentStatus { get; set; } = null!;
    public byte? RefundReasonId { get; set; }
    public string? RefundReasonContent { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public string? RequestedByName { get; set; }
    public int? RequestedBy { get; set; }
    public int? ApprovedBy { get; set; }
    public string? ReasonDetails { get; set; }
    public decimal ApprovedAmount { get; set; }
    public string RefundStatus { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<string> Images { get; set; } = new List<string>();
}
