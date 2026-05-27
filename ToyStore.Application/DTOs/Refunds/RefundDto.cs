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
    public string? RefundCode { get; set; }
    public string? ShippingOrderCode { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? AdminNote { get; set; }
    public string? AssignedToStaffName { get; set; }
    public string? AssignedToMerchName { get; set; }

    public List<string> Images { get; set; } = new List<string>();
    public List<RefundDetailDto> Details { get; set; } = new List<RefundDetailDto>();
    public List<RefundStatusHistoryDto> StatusHistory { get; set; } = new List<RefundStatusHistoryDto>();
}

public class RefundDetailDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public short Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal RefundAmount { get; set; }
}

public class RefundStatusHistoryDto
{
    public string StatusName { get; set; } = null!;
    public string? ChangedByName { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
