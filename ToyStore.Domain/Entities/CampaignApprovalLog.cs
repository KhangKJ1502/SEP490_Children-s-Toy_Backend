using System;

namespace ToyStore.Domain.Entities;

public partial class CampaignApprovalLog
{
    public int LogId { get; set; }

    public int CampaignId { get; set; }

    public string Action { get; set; } = null!;

    public int ActorId { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Campaign Campaign { get; set; } = null!;

    public virtual Account Actor { get; set; } = null!;
}
