using System;

namespace ToyStore.Domain.Entities;

public partial class RefundDetail
{
    public int RefundDetailId { get; set; }

    public int RefundId { get; set; }

    public int ProductId { get; set; }

    public short Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal RefundAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual OrderRefund Refund { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
