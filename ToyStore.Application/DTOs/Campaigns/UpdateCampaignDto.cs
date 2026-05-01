namespace ToyStore.Application.DTOs.Campaigns;

public class UpdateCampaignDto
{
    public int CampaignId { get; set; }

    public string CampaignName { get; set; } = string.Empty;

    public string? TemplateCode { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public string? TitleOverride { get; set; }

    public string? MessageOverride { get; set; }

    public string TargetType { get; set; } = "ALL";

    public DateTime? ScheduledAt { get; set; }

    public string? ImageUrl { get; set; }

    public string? ActionType { get; set; }

    public string? ActionTarget { get; set; }

    public List<CreateCampaignTargetDto> Targets { get; set; } = new();
}
