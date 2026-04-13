using ToyStore.Domain.Enums;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Represents a customer order.
/// </summary>
public class Order : AuditableEntity
{
    /// <summary>
    /// Unique order number for display.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Customer who placed the order.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Customer navigation property.
    /// </summary>
    public virtual User User { get; set; } = null!;
    
    /// <summary>
    /// Current order status.
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    /// <summary>
    /// Order date and time.
    /// </summary>
    public DateTime OrderDate { get; set; }
    
    /// <summary>
    /// Subtotal before discounts and shipping.
    /// </summary>
    public decimal SubTotal { get; set; }
    
    /// <summary>
    /// Discount amount applied.
    /// </summary>
    public decimal DiscountAmount { get; set; }
    
    /// <summary>
    /// Discount code used.
    /// </summary>
    public string? DiscountCode { get; set; }
    
    /// <summary>
    /// Shipping cost.
    /// </summary>
    public decimal ShippingCost { get; set; }
    
    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }
    
    /// <summary>
    /// Total order amount.
    /// </summary>
    public decimal TotalAmount { get; set; }
    
    /// <summary>
    /// Currency code (e.g., VND, USD).
    /// </summary>
    public string Currency { get; set; } = "VND";
    
    /// <summary>
    /// Shipping address for the order.
    /// </summary>
    public string ShippingAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Billing address for the order.
    /// </summary>
    public string? BillingAddress { get; set; }
    
    /// <summary>
    /// Recipient name.
    /// </summary>
    public string RecipientName { get; set; } = string.Empty;
    
    /// <summary>
    /// Recipient phone number.
    /// </summary>
    public string RecipientPhone { get; set; } = string.Empty;
    
    /// <summary>
    /// Order notes from customer.
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Payment method (e.g., COD, VNPay, Momo).
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;
    
    /// <summary>
    /// Payment status.
    /// </summary>
    public string PaymentStatus { get; set; } = "Pending";
    
    /// <summary>
    /// Payment transaction ID from payment gateway.
    /// </summary>
    public string? PaymentTransactionId { get; set; }
    
    /// <summary>
    /// Date and time when payment was received.
    /// </summary>
    public DateTime? PaidAt { get; set; }
    
    /// <summary>
    /// Shipping carrier name.
    /// </summary>
    public string? ShippingCarrier { get; set; }
    
    /// <summary>
    /// Tracking number for shipment.
    /// </summary>
    public string? TrackingNumber { get; set; }
    
    /// <summary>
    /// Estimated delivery date.
    /// </summary>
    public DateTime? EstimatedDeliveryDate { get; set; }
    
    /// <summary>
    /// Actual delivery date.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }
    
    /// <summary>
    /// Date when order was cancelled.
    /// </summary>
    public DateTime? CancelledAt { get; set; }
    
    /// <summary>
    /// Reason for cancellation.
    /// </summary>
    public string? CancellationReason { get; set; }
    
    /// <summary>
    /// Order line items.
    /// </summary>
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    
    /// <summary>
    /// Generates a unique order number.
    /// </summary>
    public static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}
