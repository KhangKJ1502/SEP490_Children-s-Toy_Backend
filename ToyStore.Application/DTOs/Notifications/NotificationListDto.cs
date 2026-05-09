namespace ToyStore.Application.DTOs.Notifications;

public class NotificationListDto
{
    public long DeliveryId { get; set; }
    public string NotificationType { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string? ActionType { get; set; }
    public string? ActionTarget { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
