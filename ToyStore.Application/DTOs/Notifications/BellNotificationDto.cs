namespace ToyStore.Application.DTOs.Notifications;

public record BellNotificationDto
{
    public long DeliveryId { get; init; }
    public string NotificationType { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string Message { get; init; } = default!;
    public string? ImageUrl { get; init; }
    public string? ActionType { get; init; }
    public string? ActionTarget { get; init; }
    public DateTime CreatedAt { get; init; }
    public int UnreadCount { get; init; }
}
