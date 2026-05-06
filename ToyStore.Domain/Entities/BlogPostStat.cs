using System;

namespace ToyStore.Domain.Entities;

public partial class BlogPostStat
{
    public int BlogPostId { get; set; }

    public int LikeCount { get; set; }

    public int CommentCount { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual BlogPost BlogPost { get; set; } = null!;
}
