namespace ToyStore.Application.DTOs.Notifications;

public record NotificationContext
{
    public required int RecipientAccountId { get; init; }
    public required string RecipientType { get; init; }
    public required string NotificationType { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public bool SendBell { get; init; } = true;
    public bool SendEmail { get; init; } = false;
    public string? TemplateCode { get; init; }
    public string? ImageUrl { get; init; }
    public string? ActionType { get; init; }
    public string? ActionTarget { get; init; }
    public Dictionary<string, object>? Payload { get; init; }
    public string? IdempotencyKey { get; init; }
    public int? CampaignId { get; init; }
}
