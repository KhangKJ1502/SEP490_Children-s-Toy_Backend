using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class Campaign
{
    public int CampaignId { get; set; }

    public string CampaignName { get; set; } = null!;

    public string? TemplateCode { get; set; }

    public string? TitleOverride { get; set; }

    public string? MessageOverride { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public string SourceType { get; set; } = null!;

    public string TargetType { get; set; } = null!;

    public int? SubmittedByAccountId { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int? ReviewedByAccountId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public string Status { get; set; } = null!;

    public string? EventKey { get; set; }

    public string? ImageUrl { get; set; }

    public string? ActionType { get; set; }

    public string? ActionTarget { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public DateTime? ApprovedExpireAt { get; set; }

    public byte RescheduleCount { get; set; }

    public byte MaxRescheduleCount { get; set; }

    public bool IsDeleted { get; set; }

    public int? CreatedByAccountId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual CampaignStat? CampaignStat { get; set; }

    public virtual ICollection<CampaignTarget> CampaignTargets { get; set; } = new List<CampaignTarget>();

    public virtual Account? CreatedByAccount { get; set; }

    public virtual Account? SubmittedByAccount { get; set; }

    public virtual Account? ReviewedByAccount { get; set; }

    public virtual ICollection<CampaignApprovalLog> CampaignApprovalLogs { get; set; } = new List<CampaignApprovalLog>();

    public virtual CampaignSchedule? CampaignSchedule { get; set; }

    public virtual ICollection<CampaignScheduleLog> CampaignScheduleLogs { get; set; } = new List<CampaignScheduleLog>();

    public virtual ICollection<CampaignReferenceSnapshot> CampaignReferenceSnapshots { get; set; } = new List<CampaignReferenceSnapshot>();

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual Template? TemplateCodeNavigation { get; set; }
}
