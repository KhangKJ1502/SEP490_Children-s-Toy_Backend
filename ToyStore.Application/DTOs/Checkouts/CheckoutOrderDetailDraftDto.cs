namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Order detail draft for persistence.
/// </summary>
public class CheckoutOrderDetailDraftDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? ProductImage { get; set; }

    public short Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }
}