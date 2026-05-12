using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class ReactionType
{
    public int ReactionTypeId { get; set; }

    public string Code { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BlogPostReaction> BlogPostReactions { get; set; } = new List<BlogPostReaction>();

    public virtual ICollection<ReviewBlogReaction> ReviewBlogReactions { get; set; } = new List<ReviewBlogReaction>();

    public virtual ICollection<ReviewBlogReplyReaction> ReviewBlogReplyReactions { get; set; } = new List<ReviewBlogReplyReaction>();

    public virtual ICollection<ReviewProductReaction> ReviewProductReactions { get; set; } = new List<ReviewProductReaction>();
}
