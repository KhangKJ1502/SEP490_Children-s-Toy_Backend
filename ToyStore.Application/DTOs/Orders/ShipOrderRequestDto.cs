namespace ToyStore.Application.DTOs.Orders;

/// <summary>
/// Request body cho PATCH /admin/orders/{id}/ship.
/// </summary>
public class ShipOrderRequestDto
{
    public string Provider { get; set; } = string.Empty;
    public string? ServiceType { get; set; }
    public string? Note { get; set; }
    public string? RequiredNote { get; set; }
}

/// <summary>
/// Response cho ship order.
/// </summary>
public class ShipOrderResponseDto
{
    public string? TrackingNumber { get; set; }
    public string? ProviderOrderCode { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    public decimal ShippingFee { get; set; }
}
