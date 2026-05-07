namespace ToyStore.Application.DTOs.Campaigns;

public class CampaignQueryDto
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Tim kiem tren: CampaignName, TemplateCode, EventKey.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Loc theo trang thai: Draft | Scheduled | Sending | Sent | Cancelled | Failed.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Loc theo kenh gui: Email | System | Push.
    /// </summary>
    public string? SourceType { get; set; }

    /// <summary>
    /// Loc campaign duoc tao tu ngay nay (theo CreatedAt, UTC, inclusive).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Loc campaign duoc tao den ngay nay (theo CreatedAt, UTC, inclusive – het ngay).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Truong sap xep: createdAt | name | status.
    /// Mac dinh: createdAt.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// True = giam dan, False = tang dan.
    /// </summary>
    public bool SortDesc { get; set; } = false;
}
