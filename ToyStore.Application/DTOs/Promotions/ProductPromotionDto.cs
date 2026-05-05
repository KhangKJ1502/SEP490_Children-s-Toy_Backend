namespace ToyStore.Application.DTOs.Promotions;

public class ProductPromotionDto
{
    public int ProductId { get; set; }

    // Dùng để hiển thị tên sản phẩm trong danh sách Product Promotion của 1 Promotion
    public string ProductName { get; set; } = string.Empty;

    public decimal OriginalPrice { get; set; }

    public decimal SalePrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public int? SaleQuantity { get; set; }

    public int SoldQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public int Stock { get; set; }

    public bool IsActive { get; set; }
}
