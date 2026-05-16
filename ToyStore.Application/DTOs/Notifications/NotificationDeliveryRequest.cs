namespace ToyStore.Application.DTOs.Notifications;

/// <summary>
/// Dữ liệu đã được chuẩn bị đầy đủ để một INotificationChannel thực hiện gửi.
/// Title và Message ở đây đã được render từ template (hoặc do campaign cung cấp sẵn).
/// </summary>
public record NotificationDeliveryRequest
{
    public required int AccountId { get; init; }
    public required string RecipientType { get; init; }
    public required string NotificationType { get; init; }
    public required string Channel { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string? TemplateCode { get; init; }
    public string? IdempotencyKey { get; init; }
    public string? PayloadJson { get; init; }
    public string? ImageUrl { get; init; }
    public string? ActionType { get; init; }
    public string? ActionTarget { get; init; }
    public int? CampaignId { get; init; }
}
