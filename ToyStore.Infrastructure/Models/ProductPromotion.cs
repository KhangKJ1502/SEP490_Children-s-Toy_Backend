using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class ProductPromotion
{
    public int ProductId { get; set; }

    public int PromotionId { get; set; }

    public decimal SalePrice { get; set; }

    public int? SaleQuantity { get; set; }

    public int SoldQuantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Promotion Promotion { get; set; } = null!;
}
