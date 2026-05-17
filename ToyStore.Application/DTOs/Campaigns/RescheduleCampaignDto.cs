namespace ToyStore.Application.DTOs.Campaigns;

public class RescheduleCampaignDto
{
    public DateTime NewScheduledAt { get; set; }

    public string? Reason { get; set; }
}
