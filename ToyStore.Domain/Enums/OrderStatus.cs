namespace ToyStore.Domain.Enums;

/// <summary>
/// Represents the current status of an order in the system.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order has been created but not yet confirmed</summary>
    Pending = 0,
    
    /// <summary>Order has been confirmed and payment received</summary>
    Confirmed = 1,
    
    /// <summary>Order is being processed for shipment</summary>
    Processing = 2,
    
    /// <summary>Order has been shipped</summary>
    Shipped = 3,
    
    /// <summary>Order has been delivered to customer</summary>
    Delivered = 4,
    
    /// <summary>Order has been cancelled</summary>
    Cancelled = 5,
    
    /// <summary>Order has been refunded</summary>
    Refunded = 6
}
