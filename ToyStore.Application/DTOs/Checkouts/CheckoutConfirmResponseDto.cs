namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Response payload after checkout confirmation.
/// </summary>
public class CheckoutConfirmResponseDto
{
    public int OrderId { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public string ShippingOrderCode { get; set; } = string.Empty;

    public decimal ShippingFee { get; set; }

    public DateTime EstimatedDeliveryTime { get; set; }

    public decimal TotalAmount { get; set; }
}