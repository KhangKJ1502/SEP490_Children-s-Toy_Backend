using System.Text.Json;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Notifications;

public class DomainEventPublisher : IDomainEventPublisher
{
    private readonly SEP490ToyStoreContext _db;

    public DomainEventPublisher(SEP490ToyStoreContext db)
    {
        _db = db;
    }

    public async Task PublishAsync(
        string aggregateType,
        string aggregateId,
        string eventType,
        object payload,
        CancellationToken ct = default)
    {
        var outboxEvent = new DomainEventOutbox
        {
            EventId       = Guid.NewGuid(),
            AggregateType = aggregateType,
            AggregateId   = aggregateId,
            EventType     = eventType,
            Payload       = JsonSerializer.Serialize(payload),
            OccurredOn    = DateTime.UtcNow,
            Attempts      = 0,
        };

        _db.DomainEventOutboxes.Add(outboxEvent);
        await _db.SaveChangesAsync(ct);
    }
}
