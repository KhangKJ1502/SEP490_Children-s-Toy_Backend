using System;

namespace ToyStore.Domain.Entities;

public partial class CampaignReferenceSnapshot
{
    public int SnapshotId { get; set; }

    public int CampaignId { get; set; }

    public string ReferenceType { get; set; } = null!;

    public int ReferenceId { get; set; }

    public string EntityStatus { get; set; } = null!;

    public DateTime? EntityStartDate { get; set; }

    public DateTime EntityEndDate { get; set; }

    public bool IsStale { get; set; }

    public string? StaleReason { get; set; }

    public DateTime? StaleDetectedAt { get; set; }

    public DateTime SnapshotAt { get; set; }

    public virtual Campaign Campaign { get; set; } = null!;
}
