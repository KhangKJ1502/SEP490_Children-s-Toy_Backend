namespace ToyStore.Application.DTOs.Campaigns;

public class CampaignListDto
{
    public int CampaignId { get; set; }

    public string CampaignName { get; set; } = string.Empty;

    public string SourceType { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? ScheduledAt { get; set; }

    public string? ImageUrl { get; set; }

    public string? TemplateCode { get; set; }

    public string? ReferenceType { get; set; }

    public int? CreatedByAccountId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public byte RescheduleCount { get; set; }

    public byte MaxRescheduleCount { get; set; }
}
