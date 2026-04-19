using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class ReviewBlogReaction
{
    public int ReactionBlogId { get; set; }

    public int ReviewBlogId { get; set; }

    public int AccountId { get; set; }

    public int ReactionTypeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ReactionType ReactionType { get; set; } = null!;

    public virtual ReviewBlog ReviewBlog { get; set; } = null!;
}
