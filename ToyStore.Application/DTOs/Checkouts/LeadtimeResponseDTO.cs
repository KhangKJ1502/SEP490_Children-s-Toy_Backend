namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Leadtime result transformed from GHN unix timestamp.
/// </summary>
public class LeadtimeResponseDTO
{
    public long LeadtimeUnix { get; set; }

    public DateTime EstimatedDeliveryTime { get; set; }
}