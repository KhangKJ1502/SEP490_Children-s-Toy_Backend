using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Gắn một sản phẩm cụ thể vào một PromotionTimeSlot trong FLASH_SALE.
/// Lưu giá sale và số lượng riêng cho từng slot-product pair.
/// </summary>
public partial class PromotionProductSlot
{
    public int SlotProductId { get; set; }

    public int TimeSlotId { get; set; }

    public int ProductId { get; set; }

    /// <summary>Giá bán flash-sale cho slot này.</summary>
    public decimal SalePrice { get; set; }

    /// <summary>% giảm giá (tính toán sẵn để hiển thị nhanh).</summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>Số lượng dành riêng cho slot này (bắt buộc với FLASH_SALE).</summary>
    public int SaleQuantity { get; set; }

    public int SoldQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    [NotMapped]
    public bool IsActive
    {
        get => !IsDeleted;
        set => IsDeleted = !value;
    }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual PromotionTimeSlot TimeSlot { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
