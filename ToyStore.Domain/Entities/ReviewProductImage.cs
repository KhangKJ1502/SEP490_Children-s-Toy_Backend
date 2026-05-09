using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class ReviewProductImage
{
    public int ReviewProductImageId { get; set; }

    public int ReviewProductId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string ModerationStatus { get; set; } = null!;


    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ReviewProduct ReviewProduct { get; set; } = null!;

    public virtual ICollection<ReviewModerationLog> ReviewModerationLogs { get; set; } = new List<ReviewModerationLog>();
}
