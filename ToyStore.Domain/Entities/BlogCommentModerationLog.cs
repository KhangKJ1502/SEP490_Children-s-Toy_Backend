using System;

namespace ToyStore.Domain.Entities;

public partial class BlogCommentModerationLog
{
    public int LogId { get; set; }

    public string TargetType { get; set; } = null!;

    public int? CommentId { get; set; }

    public int? ReplyId { get; set; }

    public string ModeratorType { get; set; } = null!;

    public int? ModeratedBy { get; set; }

    public string Action { get; set; } = null!;

    public byte? BanReasonId { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public string? ModerationResult { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual BlogCommentBanReason? BanReason { get; set; }

    public virtual ReviewBlog? Comment { get; set; }

    public virtual Account? ModeratedByNavigation { get; set; }

    public virtual ReviewBlogReply? Reply { get; set; }
}
