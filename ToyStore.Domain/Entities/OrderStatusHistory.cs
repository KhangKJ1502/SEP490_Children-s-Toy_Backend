using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class OrderStatusHistory
{
    public int HistoryId { get; set; }

    public int OrderId { get; set; }

    public byte StatusId { get; set; }

    public int? ChangedBy { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account? ChangedByNavigation { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual StatusOrder Status { get; set; } = null!;
}
