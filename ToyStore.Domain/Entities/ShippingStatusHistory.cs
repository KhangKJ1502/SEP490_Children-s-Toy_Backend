using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class ShippingStatusHistory
{
    public long HistoryId { get; set; }

    public long ShippingTxId { get; set; }

    public int OrderId { get; set; }

    public string PreviousStatus { get; set; } = null!;

    public string NewStatus { get; set; } = null!;

    public string Source { get; set; } = null!;

    public string? RawPayload { get; set; }

    public DateTime ProcessedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ShippingProviderTransaction ShippingTx { get; set; } = null!;
}
