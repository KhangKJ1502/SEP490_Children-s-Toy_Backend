using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class BlogPost
{
    public int BlogPostId { get; set; }

    public int AccountId { get; set; }

    public int? ApprovedBy { get; set; }

    public short BlogCategoryId { get; set; }

    public string BlogTitle { get; set; } = null!;

    public string BlogContent { get; set; } = null!;

    public string? BlogThumbnail { get; set; }

    public string Status { get; set; } = null!;

    public string? Reason { get; set; }

    public bool IsFeatured { get; set; }

    public DateTime? BlogAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Account? ApprovedByNavigation { get; set; }

    public virtual BlogCategory BlogCategory { get; set; } = null!;

    public virtual BlogPostStat? BlogPostStat { get; set; }

    public virtual ICollection<BlogPostReaction> BlogPostReactions { get; set; } = new List<BlogPostReaction>();

    public virtual ICollection<ReviewBlog> ReviewBlogs { get; set; } = new List<ReviewBlog>();
}
