namespace ToyStore.Application.DTOs.Campaigns;

/// <summary>
/// Metadata about a supported reference type — returned by GET /api/campaigns/reference-types
/// so the frontend knows which placeholders each type exposes.
/// </summary>
public class ReferenceTypeDto
{
    public string ReferenceType { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public List<PlaceholderInfoDto> Placeholders { get; set; } = new();
}

public class PlaceholderInfoDto
{
    public string Token { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Resolved data from a business object — embedded inside CampaignDto.
/// </summary>
public class ResolvedReferenceDto
{
    public string? DisplayName { get; set; }

    public Dictionary<string, string> Placeholders { get; set; } = new();

    public string? DefaultActionTarget { get; set; }
}
