using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class UserBlockHistory
{
    public int BlockId { get; set; }

    public int AccountId { get; set; }

    public int? BlockedBy { get; set; }

    public byte BlockReasonId { get; set; }

    public string? Note { get; set; }

    public int? UnblockedBy { get; set; }

    public int? UnblockedByJobId { get; set; }

    public DateTime BlockedAt { get; set; }

    public DateTime BlockedUntil { get; set; }

    public DateTime? UnblockedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual BlockReason BlockReason { get; set; } = null!;

    public virtual Account? BlockedByNavigation { get; set; }

    public virtual BackgroundJob? UnblockedByJob { get; set; }

    public virtual Account? UnblockedByNavigation { get; set; }
}
