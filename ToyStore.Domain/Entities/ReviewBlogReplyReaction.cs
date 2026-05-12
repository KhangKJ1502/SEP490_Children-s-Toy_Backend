using System;

namespace ToyStore.Domain.Entities;

public partial class ReviewBlogReplyReaction
{
    public int ReactionReplyBlogId { get; set; }

    public int ReplyBlogId { get; set; }

    public int AccountId { get; set; }

    public int ReactionTypeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ReactionType ReactionType { get; set; } = null!;

    public virtual ReviewBlogReply ReplyBlog { get; set; } = null!;
}
