using System;

namespace ToyStore.Domain.Entities;

public partial class BlogPostReaction
{
    public int ReactionPostId { get; set; }

    public int BlogPostId { get; set; }

    public int AccountId { get; set; }

    public int ReactionTypeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual BlogPost BlogPost { get; set; } = null!;

    public virtual ReactionType ReactionType { get; set; } = null!;
}
