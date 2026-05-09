namespace ToyStore.Application.Interfaces.Notifications;

public record OutboxEventData(
    Guid EventId,
    string AggregateType,
    string AggregateId,
    string EventType,
    string Payload,
    DateTime OccurredOn);

public interface IOutboxEventHandler
{
    string EventType { get; }
    Task HandleAsync(OutboxEventData eventData, CancellationToken ct);
}
