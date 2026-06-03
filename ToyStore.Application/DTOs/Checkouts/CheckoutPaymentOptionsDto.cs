namespace ToyStore.Application.DTOs.Checkouts;

public class CheckoutPaymentOptionsDto
{
    public bool IsCodRestricted { get; set; }

    public int SuspiciousDeliveryFailOrderCount { get; set; }

    public string? CodRestrictionReason { get; set; }
}
