using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class ReviewProductReaction
{
    public int ReactionProductId { get; set; }

    public int ReviewProductId { get; set; }

    public int AccountId { get; set; }

    public int ReactionTypeId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ReviewProduct ReviewProduct { get; set; } = null!;

    public virtual ReactionType ReactionTypeNavigation { get; set; } = null!;
}
