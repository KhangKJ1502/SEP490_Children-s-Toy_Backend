namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Item payload for checkout confirmation.
/// </summary>
public class CheckoutConfirmItemDto
{
    public int ProductId { get; set; }

    public short Quantity { get; set; }
}