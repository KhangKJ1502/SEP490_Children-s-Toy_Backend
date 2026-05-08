namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Fee result returned from GHN.
/// </summary>
public class FeeResponseDTO
{
    public decimal Fee { get; set; }

    public int ServiceId { get; set; }
}