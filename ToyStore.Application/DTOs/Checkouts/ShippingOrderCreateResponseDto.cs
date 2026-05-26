namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Response payload from GHN create order API.
/// </summary>
public class ShippingOrderCreateResponseDto
{
    public string OrderCode { get; set; } = string.Empty;

    public string? SortCode { get; set; }
    
    public DateTime? ExpectedDeliveryTime { get; set; }

    public decimal TotalFee { get; set; }
}