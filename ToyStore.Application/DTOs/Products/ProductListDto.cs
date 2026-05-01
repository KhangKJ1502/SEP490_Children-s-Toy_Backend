namespace ToyStore.Application.DTOs.Products;

public class ProductListDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string ProductStatus { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public short CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public short? BrandId { get; set; }

    public string? BrandName { get; set; }

    public string? MainImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
