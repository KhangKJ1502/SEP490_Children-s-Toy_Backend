using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class ReviewBlog
{
    public int ReviewBlogId { get; set; }

    public int BlogPostId { get; set; }

    public int AccountId { get; set; }

    public string? Comment { get; set; }

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

    public virtual BlogPost BlogPost { get; set; } = null!;

    public virtual Account? HiddenByNavigation { get; set; }

    public virtual ICollection<BlogCommentModerationLog> BlogCommentModerationLogs { get; set; } = new List<BlogCommentModerationLog>();

    public virtual ICollection<ReviewBlogReaction> ReviewBlogReactions { get; set; } = new List<ReviewBlogReaction>();

    public virtual ICollection<ReviewBlogReply> ReviewBlogReplies { get; set; } = new List<ReviewBlogReply>();
}
