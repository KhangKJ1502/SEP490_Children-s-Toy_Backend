using ToyStore.Application.Common.Helpers;

namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Request payload for GHN fee API.
/// </summary>
public class FeeRequestDTO
{
    public int FromDistrictId { get; set; }

    public string FromWardCode { get; set; } = string.Empty;

    public int? ServiceTypeId { get; set; }

    public int ToDistrictId { get; set; }

    public string ToWardCode { get; set; } = string.Empty;

    public decimal InsuranceValue { get; set; }

    public decimal CodValue { get; set; }

    public int Weight { get; set; }

    public int Length { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    /// <summary>
    /// Optional item list — mirrors CreateOrderAsync payload so GHN calculates fee identically.
    /// </summary>
    public List<GhnItem>? Items { get; set; }
}