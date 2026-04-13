using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class ReviewProductReply
{
    public int ReplyProductId { get; set; }

    public int ReviewProductId { get; set; }

    public int AccountId { get; set; }

    public string Content { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ReviewProduct ReviewProduct { get; set; } = null!;
}
