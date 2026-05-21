using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class ReviewBlogReply
{
    public int ReplyBlogId { get; set; }

    public int ReviewBlogId { get; set; }

    public int AccountId { get; set; }

    public int? ParentReplyId { get; set; }

    public int? ReplyToAccountId { get; set; }

    public string Comment { get; set; } = null!;

    public string ModerationStatus { get; set; } = null!;

    public DateTime? ManualReviewDeadline { get; set; }

    public byte RetryCount { get; set; }

    public DateTime? LastRetryAt { get; set; }

    public bool IsHidden { get; set; }

    public int? HiddenBy { get; set; }

    public DateTime? HiddenAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Account? HiddenByNavigation { get; set; }

    public virtual ICollection<ReviewBlogReply> InverseParentReply { get; set; } = new List<ReviewBlogReply>();

    public virtual ReviewBlogReply? ParentReply { get; set; }

    public virtual Account? ReplyToAccount { get; set; }

    public virtual ReviewBlog ReviewBlog { get; set; } = null!;

    public virtual ICollection<BlogCommentModerationLog> BlogCommentModerationLogs { get; set; } = new List<BlogCommentModerationLog>();

    public virtual ICollection<ReviewBlogReplyReaction> ReviewBlogReplyReactions { get; set; } = new List<ReviewBlogReplyReaction>();
}
