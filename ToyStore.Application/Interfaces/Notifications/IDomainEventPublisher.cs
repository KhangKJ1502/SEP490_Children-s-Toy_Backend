namespace ToyStore.Application.Interfaces.Notifications;

public interface IDomainEventPublisher
{
    /// <summary>
    /// Ghi một domain event vào [System].[DomainEventOutbox].
    /// Gọi sau khi nghiệp vụ đã commit thành công trong cùng scope.
    /// </summary>
    Task PublishAsync(
        string aggregateType,
        string aggregateId,
        string eventType,
        object payload,
        CancellationToken ct = default);
}
