namespace ToyStore.Domain.Enums;

/// <summary>
/// Represents the current status of an order in the system.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order has been created but not yet confirmed</summary>
    Pending = 1,
    
    /// <summary>Order has been confirmed and payment received</summary>
    Confirmed = 2,
    
    /// <summary>Order is being processed for shipment</summary>
    Processing = 3,
    
    /// <summary>Order has been shipped</summary>
    Shipped = 4,

    /// <summary>Order is being delivered to customer</summary>
    Delivering = 5,
    
    /// <summary>Order has been delivered to customer</summary>
    Delivered = 6,

    /// <summary>Order has been completed (received and no complaints)</summary>
    Completed = 7,
    
    /// <summary>Order has been cancelled</summary>
    Cancelled = 8,
    
    /// <summary>Order has been refunded</summary>
    Refunded = 9
}
