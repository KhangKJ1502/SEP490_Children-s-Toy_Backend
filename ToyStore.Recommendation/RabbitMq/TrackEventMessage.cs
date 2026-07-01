using System;

namespace ToyStore.Recommendation.RabbitMq;

public class TrackEventMessage
{
    public Guid IdempotencyKey { get; set; }
    public int? AccountId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Referrer { get; set; }
    public string? DeviceType { get; set; }
    public int? DurationMs { get; set; }
    public byte? ScrollDepth { get; set; }
    public string? ClickPosition { get; set; }
    public string? Metadata { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
