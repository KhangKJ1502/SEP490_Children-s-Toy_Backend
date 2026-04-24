using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class OrderRefund
{
    public int RefundId { get; set; }

    public int OrderId { get; set; }

    public byte? RefundReasonId { get; set; }

    public int CustomerId { get; set; }

    public int? RequestedBy { get; set; }

    public int? ApprovedBy { get; set; }

    public int? WalletTransactionId { get; set; }

    public string? ComplaintImageUrl { get; set; }

    public string? ReasonDetails { get; set; }

    public decimal ApprovedAmount { get; set; }

    public string RefundStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account? ApprovedByNavigation { get; set; }

    public virtual Account Customer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual OrderRefundReason? RefundReason { get; set; }

    public virtual Account? RequestedByNavigation { get; set; }

    public virtual WalletTransaction? WalletTransaction { get; set; }
}
