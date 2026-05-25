namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// DTO đọc dữ liệu sản phẩm tham gia một time slot FLASH_SALE.
/// </summary>
public class PromotionProductSlotDto
{
    public int SlotProductId { get; set; }

    public int TimeSlotId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    /// <summary>URL ảnh chính của sản phẩm (để hiển thị trong Flash Sale grid).</summary>
    public string? MainImageUrl { get; set; }

    public decimal OriginalPrice { get; set; }

    /// <summary>Giá bán flash-sale trong slot này.</summary>
    public decimal SalePrice { get; set; }

    /// <summary>% giảm giá (tính toán sẵn).</summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>Số lượng dành riêng cho slot (bắt buộc).</summary>
    public int SaleQuantity { get; set; }

    public int SoldQuantity { get; set; }

    public int ReservedQuantity { get; set; }

}
