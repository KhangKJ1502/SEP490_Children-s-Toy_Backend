using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class OrderRefundReason
{
    public byte RefundReasonId { get; set; }

    public string Content { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<OrderRefund> OrderRefunds { get; set; } = new List<OrderRefund>();
}
