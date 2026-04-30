using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class StaffReviewProductReply
{
    public int ReplyProductId { get; set; }

    public int ReviewProductId { get; set; }

    public int StaffId { get; set; }

    public string Content { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ReviewProduct ReviewProduct { get; set; } = null!;

    public virtual Account Staff { get; set; } = null!;
}
