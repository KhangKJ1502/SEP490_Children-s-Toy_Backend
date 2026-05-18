namespace ToyStore.Application.DTOs.Campaigns;

/// <summary>
/// DTO dùng khi Staff đặt lịch gửi Campaign (sau khi Admin đã duyệt).
/// </summary>
public class ScheduleCampaignDto
{
    /// <summary>
    /// Thời điểm sẽ gửi Campaign (UTC). Bắt buộc khi schedule lần đầu sau Approved.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }
}
