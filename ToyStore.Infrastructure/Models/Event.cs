using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Event
{
    public long EventId { get; set; }

    public int? AccountId { get; set; }

    public string SessionId { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Account? Account { get; set; }
}
