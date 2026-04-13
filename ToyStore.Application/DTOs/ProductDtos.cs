using ToyStore.Domain.Enums;

namespace ToyStore.Application.DTOs;

/// <summary>
/// Product data transfer object for read operations.
/// </summary>
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal EffectivePrice { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public ToyCategory ToyCategory { get; set; }
    public AgeRange AgeRange { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> AdditionalImages { get; set; } = new();
    public int StockQuantity { get; set; }
    public bool IsInStock { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsNewArrival { get; set; }
    public decimal? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for creating a new product.
/// </summary>
public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? CostPrice { get; set; }
    public Guid CategoryId { get; set; }
    public ToyCategory ToyCategory { get; set; }
    public AgeRange AgeRange { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public string? Brand { get; set; }
    public decimal? Weight { get; set; }
    public string? Dimensions { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? AdditionalImages { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 10;
    public bool IsFeatured { get; set; }
    public bool IsNewArrival { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Request DTO for updating a product.
/// </summary>
public class UpdateProductDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public decimal? Price { get; set; }
    public decimal? SalePrice { get; set; }
    public Guid? CategoryId { get; set; }
    public ToyCategory? ToyCategory { get; set; }
    public AgeRange? AgeRange { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? AdditionalImages { get; set; }
    public int? StockQuantity { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsFeatured { get; set; }
    public bool? IsNewArrival { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Simplified product DTO for listings.
/// </summary>
public class ProductListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public string? ImageUrl { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public AgeRange AgeRange { get; set; }
    public decimal? AverageRating { get; set; }
    public bool IsInStock { get; set; }
    public bool IsFeatured { get; set; }
}
