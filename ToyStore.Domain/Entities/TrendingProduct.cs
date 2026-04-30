using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class TrendingProduct
{
    public int TrendingId { get; set; }

    public int ProductId { get; set; }

    public string Scope { get; set; } = null!;

    public decimal Score { get; set; }

    public int ViewCount { get; set; }

    public int PurchaseCount { get; set; }

    public short Rank { get; set; }

    public byte WindowHours { get; set; }

    public DateTime ComputedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
