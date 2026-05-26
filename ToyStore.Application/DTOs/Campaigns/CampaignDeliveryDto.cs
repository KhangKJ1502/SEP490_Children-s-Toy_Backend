namespace ToyStore.Application.DTOs.Campaigns;

/// <summary>
/// DTO chua thong tin 1 delivery (nguoi nhan) cua mot Campaign.
/// </summary>
public class CampaignDeliveryDto
{
    public long DeliveryId { get; set; }

    public int AccountId { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsClicked { get; set; }
}
