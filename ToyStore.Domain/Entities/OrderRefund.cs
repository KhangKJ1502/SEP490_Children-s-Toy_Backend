using System;
using System.Collections.Generic;
using ToyStore.Domain.Constants;

namespace ToyStore.Domain.Entities;

public partial class OrderRefund
{
    public int RefundId { get; set; }

    public int OrderId { get; set; }

    public byte? RefundReasonId { get; set; }

    public int CustomerId { get; set; }

    public int? RequestedBy { get; set; }

    public int? ApprovedBy { get; set; }

    public int? WalletTransactionId { get; set; }

    public string? ReasonDetails { get; set; }

    /// <summary>
    /// Nguồn tạo refund: "Customer" (khách tự tạo) hoặc "System" (hệ thống tạo khi GHN returned).
    /// System refund không sử dụng GHN pickup — chỉ Approve → Complete → hoàn ví.
    /// </summary>
    public string RefundSource { get; set; } = RefundSources.Customer;

    public decimal ApprovedAmount { get; set; }

    public string RefundCode { get; set; } = null!;

    public string? ShippingOrderCode { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal? SubTotal { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? AdminNote { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public bool IsDeleted { get; set; }

    public byte StatusId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account? ApprovedByNavigation { get; set; }

    public virtual Account Customer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual OrderRefundReason? RefundReason { get; set; }

    public virtual Account? RequestedByNavigation { get; set; }

    public virtual WalletTransaction? WalletTransaction { get; set; }

    public virtual StatusRefund Status { get; set; } = null!;

    public virtual ICollection<RefundImage> RefundImages { get; set; } = new List<RefundImage>();

    public virtual ICollection<RefundDetail> RefundDetails { get; set; } = new List<RefundDetail>();

    public virtual ICollection<RefundStatusHistory> RefundStatusHistories { get; set; } = new List<RefundStatusHistory>();
}
