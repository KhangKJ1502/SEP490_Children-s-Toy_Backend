namespace ToyStore.Domain.Entities;

/// <summary>
/// Represents a line item in an order.
/// </summary>
public class OrderItem : Entity
{
    /// <summary>
    /// Order this item belongs to.
    /// </summary>
    public Guid OrderId { get; set; }
    
    /// <summary>
    /// Order navigation property.
    /// </summary>
    public virtual Order Order { get; set; } = null!;
    
    /// <summary>
    /// Product in this line item.
    /// </summary>
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// Product navigation property.
    /// </summary>
    public virtual Product Product { get; set; } = null!;
    
    /// <summary>
    /// Product name at time of order (for historical reference).
    /// </summary>
    public string ProductName { get; set; } = string.Empty;
    
    /// <summary>
    /// Product SKU at time of order.
    /// </summary>
    public string ProductSKU { get; set; } = string.Empty;
    
    /// <summary>
    /// Product image URL at time of order.
    /// </summary>
    public string? ProductImageUrl { get; set; }
    
    /// <summary>
    /// Quantity ordered.
    /// </summary>
    public int Quantity { get; set; }
    
    /// <summary>
    /// Unit price at time of order.
    /// </summary>
    public decimal UnitPrice { get; set; }
    
    /// <summary>
    /// Discount applied to this item.
    /// </summary>
    public decimal DiscountAmount { get; set; }
    
    /// <summary>
    /// Total price for this line item.
    /// </summary>
    public decimal TotalPrice { get; set; }
    
    /// <summary>
    /// Calculates the total price for this item.
    /// </summary>
    public void CalculateTotal()
    {
        TotalPrice = (UnitPrice * Quantity) - DiscountAmount;
    }
}
