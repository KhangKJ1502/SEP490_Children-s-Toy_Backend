namespace ToyStore.Application.DTOs.Campaigns;

/// <summary>
/// DTO dùng khi Admin duyệt hoặc từ chối Campaign.
/// </summary>
public class ReviewCampaignDto
{
    /// <summary>
    /// Hành động: "Approved" hoặc "Rejected".
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Ghi chú của Admin (bắt buộc khi Action = "Rejected").
    /// </summary>
    public string? ReviewNote { get; set; }
}
