namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Item payload for GHN create order API.
/// </summary>
public class ShippingOrderCreateItemDto
{
    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public int Weight { get; set; }
}