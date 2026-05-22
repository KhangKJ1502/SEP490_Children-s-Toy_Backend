using System;

namespace ToyStore.Domain.Entities;

public partial class BlogCommentViolationCount
{
    public int AccountId { get; set; }

    public byte ViolationCount { get; set; }

    public DateTime? LastViolatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsCommentBanned { get; set; }

    public DateTime? BannedAt { get; set; }

    public DateTime? BanExpiresAt { get; set; }

    public DateTime? UnbannedAt { get; set; }

    public int? UnbannedBy { get; set; }

    public byte RateCount { get; set; }

    public DateTime? RateWindowAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Account? UnbannedByNavigation { get; set; }
}
