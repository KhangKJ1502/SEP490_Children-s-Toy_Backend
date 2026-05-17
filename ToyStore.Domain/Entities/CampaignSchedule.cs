using System;

namespace ToyStore.Domain.Entities;

public partial class CampaignSchedule
{
    public int ScheduleId { get; set; }

    public int CampaignId { get; set; }

    public int ScheduledBy { get; set; }

    public DateTime ScheduledAt { get; set; }

    public int? LockedByJobId { get; set; }

    public DateTime? LockedAt { get; set; }

    public string ExecutionStatus { get; set; } = null!;

    public byte AttemptCount { get; set; }

    public byte MaxAttemptCount { get; set; }

    public string? LastError { get; set; }

    public DateTime? ExecutedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Campaign Campaign { get; set; } = null!;

    public virtual Account ScheduledByNavigation { get; set; } = null!;

    public virtual BackgroundJob? LockedByJob { get; set; }
}
