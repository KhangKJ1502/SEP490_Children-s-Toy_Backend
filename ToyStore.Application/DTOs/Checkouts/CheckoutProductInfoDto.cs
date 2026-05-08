namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Product data used for order creation.
/// </summary>
public class CheckoutProductInfoDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? ProductImage { get; set; }

    public decimal UnitPrice { get; set; }

    public int AvailableQuantity { get; set; }

    public bool IsActive { get; set; }
}