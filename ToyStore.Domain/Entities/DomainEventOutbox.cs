using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class DomainEventOutbox
{
    public Guid EventId { get; set; }

    public string AggregateType { get; set; } = null!;

    public string AggregateId { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DateTime OccurredOn { get; set; }

    public Guid? ProcessingLockId { get; set; }

    public DateTime? ProcessingAt { get; set; }

    public byte Attempts { get; set; }

    public string? LastError { get; set; }

    public DateTime? ProcessedOn { get; set; }
}
