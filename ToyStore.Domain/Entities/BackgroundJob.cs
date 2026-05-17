using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class BackgroundJob
{
    public int JobId { get; set; }

    public string JobName { get; set; } = null!;

    public string? CronExpression { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime? LastRunTime { get; set; }

    public DateTime? NextRunTime { get; set; }

    public string? LastRunStatus { get; set; }

    public string? LastRunMessage { get; set; }

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual ICollection<UserBlockHistory> UserBlockHistories { get; set; } = new List<UserBlockHistory>();

    public virtual ICollection<CampaignSchedule> CampaignSchedules { get; set; } = new List<CampaignSchedule>();
}
