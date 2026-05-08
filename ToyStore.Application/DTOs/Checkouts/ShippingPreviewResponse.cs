namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Combined shipping preview.
/// </summary>
public class ShippingPreviewResponse
{
    public decimal Fee { get; set; }

    public DateTime EstimatedDeliveryTime { get; set; }
}