namespace ToyStore.Application.DTOs.Campaigns;

public class CreateCampaignDto
{
    public string CampaignName { get; set; } = string.Empty;

    public string? TemplateCode { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public string? TitleOverride { get; set; }

    public string? MessageOverride { get; set; }

    public string SourceType { get; set; } = "ADMIN";

    public string TargetType { get; set; } = "ALL";

    public DateTime? ScheduledAt { get; set; }

    public string? EventKey { get; set; }

    public string? ImageUrl { get; set; }

    public string? ActionType { get; set; }

    public string? ActionTarget { get; set; }

    public int CreatedByAccountId { get; set; }

    public List<CreateCampaignTargetDto> Targets { get; set; } = new();
}

public class CreateCampaignTargetDto
{
    public string TargetType { get; set; } = string.Empty;

    public string TargetValue { get; set; } = string.Empty;
}
