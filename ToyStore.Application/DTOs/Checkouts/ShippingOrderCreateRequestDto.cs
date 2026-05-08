namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Request payload for GHN create order API.
/// </summary>
public class ShippingOrderCreateRequestDto
{
    public string ClientOrderCode { get; set; } = string.Empty;

    public string ToName { get; set; } = string.Empty;

    public string ToPhone { get; set; } = string.Empty;

    public string ToAddress { get; set; } = string.Empty;

    public int ToDistrictId { get; set; }

    public string ToWardCode { get; set; } = string.Empty;

    public int ServiceId { get; set; }

    public decimal InsuranceValue { get; set; }

    public decimal CodAmount { get; set; }

    public int Weight { get; set; }

    public int Length { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public string? Note { get; set; }

    public List<ShippingOrderCreateItemDto> Items { get; set; } = [];
}