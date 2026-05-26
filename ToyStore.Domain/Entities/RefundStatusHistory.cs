using System;

namespace ToyStore.Domain.Entities;

public partial class RefundStatusHistory
{
    public int HistoryId { get; set; }

    public int RefundId { get; set; }

    public byte StatusId { get; set; }

    public int? ChangedBy { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual OrderRefund Refund { get; set; } = null!;

    public virtual StatusRefund Status { get; set; } = null!;

    public virtual Account? ChangedByNavigation { get; set; }
}
