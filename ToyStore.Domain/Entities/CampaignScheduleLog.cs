using System;

namespace ToyStore.Domain.Entities;

public partial class CampaignScheduleLog
{
    public int LogId { get; set; }

    public int CampaignId { get; set; }

    public int ActorId { get; set; }

    public string Action { get; set; } = null!;

    public DateTime? PreviousScheduledAt { get; set; }

    public DateTime NewScheduledAt { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Campaign Campaign { get; set; } = null!;

    public virtual Account Actor { get; set; } = null!;
}
