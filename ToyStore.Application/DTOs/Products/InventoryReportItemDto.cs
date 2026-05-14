namespace ToyStore.Application.DTOs.Products;

public class InventoryReportItemDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string? BrandName { get; set; }

    public string ProductStatus { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal? DiscountedPrice { get; set; }

    public int? DiscountPercent { get; set; }

    public string? PromotionType { get; set; }

    public int Quantity { get; set; }

    public short StockThreshold { get; set; }

    public bool LowStock { get; set; }

    public decimal InventoryValue { get; set; }

    public int SoldQuantity { get; set; }

    public int ReviewCount { get; set; }

    public double? AverageRating { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
