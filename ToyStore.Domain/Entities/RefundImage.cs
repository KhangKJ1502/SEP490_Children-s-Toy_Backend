using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class RefundImage
{
    public int RefundImageId { get; set; }

    public int RefundId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual OrderRefund Refund { get; set; } = null!;
}
