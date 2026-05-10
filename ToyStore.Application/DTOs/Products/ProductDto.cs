namespace ToyStore.Application.DTOs.Products;

public class ProductDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal? DiscountedPrice { get; set; }

    public int? DiscountPercent { get; set; }
    
    public string? PromotionType { get; set; }
    
    public int? PromotionSoldQuantity { get; set; }
    
    public int? PromotionSaleQuantity { get; set; }

    public int Quantity { get; set; }

    public string ProductStatus { get; set; } = string.Empty;

    public DateTime? LaunchDate { get; set; }

    public short StockThreshold { get; set; }

    public bool LowStockNotificationEnabled { get; set; }

    public DateTime? LastLowStockNotifiedAt { get; set; }

    public short CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public short? BrandId { get; set; }

    public string? BrandName { get; set; }

    public byte? PriceRangeId { get; set; }

    public decimal? PriceRangeMin { get; set; }

    public decimal? PriceRangeMax { get; set; }

    public string? Description { get; set; }

    public short? MaterialId { get; set; }

    public string? MaterialName { get; set; }

    public byte? AgeId { get; set; }

    public string? AgeRange { get; set; }

    public byte? SexId { get; set; }

    public string? SexName { get; set; }

    public byte? OriginId { get; set; }

    public string? OriginName { get; set; }

    public string? MainImageUrl { get; set; }

    public List<string> AdditionalImageUrls { get; set; } = [];

    public double? AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public int SoldQuantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
