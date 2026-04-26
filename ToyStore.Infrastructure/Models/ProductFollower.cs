using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class ProductFollower
{
    public int FollowerId { get; set; }

    public int ProductId { get; set; }

    public int AccountId { get; set; }

    public DateTime? NotifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
