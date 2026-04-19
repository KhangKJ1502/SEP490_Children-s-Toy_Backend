using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Campaign
{
    public int CampaignId { get; set; }

    public string CampaignName { get; set; } = null!;

    public string? TemplateCode { get; set; }

    public string? TitleOverride { get; set; }

    public string? MessageOverride { get; set; }

    public string SourceType { get; set; } = null!;

    public string TargetType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? ScheduledAt { get; set; }

    public string? EventKey { get; set; }

    public string? ImageUrl { get; set; }

    public string? ActionType { get; set; }

    public string? ActionTarget { get; set; }

    public int CreatedByAccountId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CampaignTarget> CampaignTargets { get; set; } = new List<CampaignTarget>();

    public virtual Account CreatedByAccount { get; set; } = null!;

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual Template? TemplateCodeNavigation { get; set; }
}
