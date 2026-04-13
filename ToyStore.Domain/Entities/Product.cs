using ToyStore.Domain.Enums;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Represents a toy product in the store.
/// </summary>
public class Product : AuditableEntity
{
    /// <summary>
    /// Product name/title.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// URL-friendly slug for the product.
    /// </summary>
    public string Slug { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed product description.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Short description for listings.
    /// </summary>
    public string? ShortDescription { get; set; }
    
    /// <summary>
    /// Stock Keeping Unit - unique product identifier.
    /// </summary>
    public string SKU { get; set; } = string.Empty;
    
    /// <summary>
    /// Product price.
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Discounted price if on sale.
    /// </summary>
    public decimal? SalePrice { get; set; }
    
    /// <summary>
    /// Cost price for profit calculation.
    /// </summary>
    public decimal? CostPrice { get; set; }
    
    /// <summary>
    /// Category ID the product belongs to.
    /// </summary>
    public Guid CategoryId { get; set; }
    
    /// <summary>
    /// Product category navigation property.
    /// </summary>
    public virtual Category Category { get; set; } = null!;
    
    /// <summary>
    /// Toy category type for filtering.
    /// </summary>
    public ToyCategory ToyCategory { get; set; }
    
    /// <summary>
    /// Recommended age range for the toy.
    /// </summary>
    public AgeRange AgeRange { get; set; }
    
    /// <summary>
    /// Minimum age in years.
    /// </summary>
    public int? MinAge { get; set; }
    
    /// <summary>
    /// Maximum age in years.
    /// </summary>
    public int? MaxAge { get; set; }
    
    /// <summary>
    /// Brand/manufacturer name.
    /// </summary>
    public string? Brand { get; set; }
    
    /// <summary>
    /// Product weight in grams.
    /// </summary>
    public decimal? Weight { get; set; }
    
    /// <summary>
    /// Product dimensions (LxWxH).
    /// </summary>
    public string? Dimensions { get; set; }
    
    /// <summary>
    /// Primary product image URL.
    /// </summary>
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// Additional image URLs (JSON array).
    /// </summary>
    public string? AdditionalImages { get; set; }
    
    /// <summary>
    /// Current stock quantity.
    /// </summary>
    public int StockQuantity { get; set; }
    
    /// <summary>
    /// Low stock threshold for alerts.
    /// </summary>
    public int LowStockThreshold { get; set; } = 10;
    
    /// <summary>
    /// Whether the product is available for purchase.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Whether the product is featured on homepage.
    /// </summary>
    public bool IsFeatured { get; set; }
    
    /// <summary>
    /// Whether the product is new arrival.
    /// </summary>
    public bool IsNewArrival { get; set; }
    
    /// <summary>
    /// Average rating (1-5 stars).
    /// </summary>
    public decimal? AverageRating { get; set; }
    
    /// <summary>
    /// Total number of reviews.
    /// </summary>
    public int ReviewCount { get; set; }
    
    /// <summary>
    /// Total number of times product was purchased.
    /// </summary>
    public int PurchaseCount { get; set; }
    
    /// <summary>
    /// Total number of times product was viewed.
    /// </summary>
    public int ViewCount { get; set; }
    
    /// <summary>
    /// Search tags for better discoverability (comma-separated).
    /// </summary>
    public string? Tags { get; set; }
    
    /// <summary>
    /// Order items containing this product.
    /// </summary>
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    
    /// <summary>
    /// User behaviors related to this product.
    /// </summary>
    public virtual ICollection<UserBehavior> UserBehaviors { get; set; } = new List<UserBehavior>();
    
    /// <summary>
    /// Gets the effective price (sale price if available, otherwise regular price).
    /// </summary>
    public decimal EffectivePrice => SalePrice ?? Price;
    
    /// <summary>
    /// Checks if the product is in stock.
    /// </summary>
    public bool IsInStock => StockQuantity > 0;
    
    /// <summary>
    /// Checks if stock is low.
    /// </summary>
    public bool IsLowStock => StockQuantity <= LowStockThreshold && StockQuantity > 0;
}
