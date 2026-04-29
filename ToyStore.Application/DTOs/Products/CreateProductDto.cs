namespace ToyStore.Application.DTOs.Products;

public class CreateProductDto
{
    public short CategoryId { get; set; }

    public short? BrandId { get; set; }

    public byte? PriceRangeId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string ProductStatus { get; set; } = string.Empty;

    public DateTime? LaunchDate { get; set; }

    public short StockThreshold { get; set; } = 10;

    public bool LowStockNotificationEnabled { get; set; } = true;

    public string? Description { get; set; }

    public short? MaterialId { get; set; }

    public byte? AgeId { get; set; }

    public byte? SexId { get; set; }

    public byte? OriginId { get; set; }

    public string? MainImageUrl { get; set; }
}
