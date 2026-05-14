using System;

namespace ToyStore.Domain.Entities;

public partial class OrderQueue
{
    public int QueueId { get; set; }

    public int OrderId { get; set; }

    public DateTime QueuedAt { get; set; }

    /// <summary>NO_STAFF_ON_DUTY | ALL_STAFF_FULL | NO_MERCH_ON_DUTY | ALL_MERCH_FULL | BOTH_FULL</summary>
    public string Reason { get; set; } = null!;

    /// <summary>Manager đã xử lý</summary>
    public int? AssignedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public bool IsResolved { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Account? AssignedByNavigation { get; set; }
}
