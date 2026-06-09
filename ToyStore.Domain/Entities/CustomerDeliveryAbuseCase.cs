using System;

namespace ToyStore.Domain.Entities;

public partial class CustomerDeliveryAbuseCase
{
    public int CaseId { get; set; }

    public int AccountId { get; set; }

    public string Status { get; set; } = null!;

    public byte WarningLevel { get; set; }

    public int SuspiciousOrderCount { get; set; }

    public DateTime? CountingFrom { get; set; }

    public string? LastGHNFailCode { get; set; }

    public DateTime? LastSuspiciousOrderDate { get; set; }

    public DateTime? CodRestrictedAt { get; set; }

    public DateTime? ReviewRequestedAt { get; set; }

    public DateTime? BlockedAt { get; set; }

    public int? BlockedBy { get; set; }

    public DateTime? AppealReviewedAt { get; set; }

    public int? AppealReviewedBy { get; set; }

    public string? AppealDecision { get; set; }

    public DateTime? StrictPeriodUntil { get; set; }

    public DateTime? PermanentBlockedAt { get; set; }

    public string? PermanentBlockReason { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
