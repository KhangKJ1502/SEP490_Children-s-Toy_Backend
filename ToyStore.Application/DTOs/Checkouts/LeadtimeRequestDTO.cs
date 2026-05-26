namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Request payload for GHN leadtime API.
/// </summary>
public class LeadtimeRequestDTO
{
    public int FromDistrictId { get; set; }

    public string FromWardCode { get; set; } = string.Empty;

    public int ToDistrictId { get; set; }

    public string ToWardCode { get; set; } = string.Empty;

    public int? ServiceTypeId { get; set; }
}