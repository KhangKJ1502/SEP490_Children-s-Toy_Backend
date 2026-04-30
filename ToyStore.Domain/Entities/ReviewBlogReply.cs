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

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<ReviewBlogReply> InverseParentReply { get; set; } = new List<ReviewBlogReply>();

    public virtual ReviewBlogReply? ParentReply { get; set; }

    public virtual Account? ReplyToAccount { get; set; }

    public virtual ReviewBlog ReviewBlog { get; set; } = null!;
}
