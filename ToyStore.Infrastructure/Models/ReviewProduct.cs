using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class ReviewProduct
{
    public int ReviewId { get; set; }

    public int AccountId { get; set; }

    public int ProductId { get; set; }

    public int OrderId { get; set; }

    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ReviewProductImage> ReviewProductImages { get; set; } = new List<ReviewProductImage>();

    public virtual ICollection<ReviewProductReaction> ReviewProductReactions { get; set; } = new List<ReviewProductReaction>();

    public virtual ICollection<StaffReviewProductReply> StaffReviewProductReplies { get; set; } = new List<StaffReviewProductReply>();
}
