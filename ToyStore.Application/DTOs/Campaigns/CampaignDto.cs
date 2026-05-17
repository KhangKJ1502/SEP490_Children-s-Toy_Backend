namespace ToyStore.Application.DTOs.Campaigns;

public class CampaignDto
{
    public int CampaignId { get; set; }

    public string CampaignName { get; set; } = string.Empty;

    public string? TemplateCode { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public ResolvedReferenceDto? ResolvedReference { get; set; }

    public string? TitleOverride { get; set; }

    public string? MessageOverride { get; set; }

    public string? ResolvedTitle { get; set; }

    public string? ResolvedMessage { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? ScheduledAt { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public DateTime? ApprovedExpireAt { get; set; }

    public byte RescheduleCount { get; set; }

    public byte MaxRescheduleCount { get; set; }

    public string? EventKey { get; set; }

    public string? ImageUrl { get; set; }

    public string? ActionType { get; set; }

    public string? ActionTarget { get; set; }

    public int? SubmittedByAccountId { get; set; }

    public string? SubmittedByAccountName { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int? ReviewedByAccountId { get; set; }

    public string? ReviewedByAccountName { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public int? CreatedByAccountId { get; set; }

    public string? CreatedByAccountName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public CampaignStatDto? Stat { get; set; }

    public List<CampaignTargetDto> Targets { get; set; } = new();
}

public class CampaignStatDto
{
    public int StatId { get; set; }

    public int TotalSent { get; set; }

    public int TotalRead { get; set; }

    public int TotalClicked { get; set; }

    public DateTime ComputedAt { get; set; }
}

public class CampaignTargetDto
{
    public int CampaignTargetId { get; set; }

    public string TargetType { get; set; } = string.Empty;

    public string TargetValue { get; set; } = string.Empty;
}
