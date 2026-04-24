using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class CartItem
{
    public int CartItemId { get; set; }

    public int CartId { get; set; }

    public int ProductId { get; set; }

    public short Quantity { get; set; }

    public decimal PriceAtThatTime { get; set; }

    public decimal CurrentPrice { get; set; }

    public bool IsSelected { get; set; }

    public DateTime AddedAt { get; set; }

    public DateTime? RemovedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Cart Cart { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
