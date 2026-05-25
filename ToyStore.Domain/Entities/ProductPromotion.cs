using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

public partial class ProductPromotion
{
    public int ProductId { get; set; }

    public int PromotionId { get; set; }

    public decimal SalePrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public int? SaleQuantity { get; set; }

    public int SoldQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Promotion Promotion { get; set; } = null!;
}
