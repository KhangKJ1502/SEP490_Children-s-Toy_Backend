namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Request payload for checkout preview.
/// </summary>
public class CheckoutPreviewRequestDto
{
    public int AccountId { get; set; }

    public int AddressId { get; set; }

    public decimal InsuranceValue { get; set; }

    public decimal CodValue { get; set; }

    public int Weight { get; set; } = 1000;

    public int Length { get; set; } = 20;

    public int Width { get; set; } = 15;

    public int Height { get; set; } = 10;
}