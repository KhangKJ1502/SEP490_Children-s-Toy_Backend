namespace ToyStore.Application.DTOs.Campaigns;

/// <summary>Returned when scheduling succeeds; may include non-blocking warnings.</summary>
public class ScheduleCampaignResultDto
{
    public IReadOnlyList<string>? WarningCodes { get; init; }
}
