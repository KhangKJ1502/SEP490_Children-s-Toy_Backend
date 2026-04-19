using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class UserProductScore
{
    public long ScoreId { get; set; }

    public int AccountId { get; set; }

    public int ProductId { get; set; }

    public decimal Score { get; set; }

    public short ViewCount { get; set; }

    public byte CartCount { get; set; }

    public byte PurchaseCount { get; set; }

    public byte WishlistCount { get; set; }

    public DateTime LastInteractedAt { get; set; }

    public DateTime ComputedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
