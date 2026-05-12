using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? ProductImage { get; set; }

    public short Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal? LineTotal { get; set; }

    public int? PromotionId { get; set; }

    public int? SlotProductId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual Promotion? Promotion { get; set; }

    public virtual PromotionProductSlot? SlotProduct { get; set; }
}
