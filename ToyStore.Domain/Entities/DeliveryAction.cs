using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class DeliveryAction
{
    public long ActionId { get; set; }

    public long DeliveryId { get; set; }

    public int AccountId { get; set; }

    public string ActionType { get; set; } = null!;

    public string? ActionTarget { get; set; }

    public DateTime OccurredAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Delivery Delivery { get; set; } = null!;
}
