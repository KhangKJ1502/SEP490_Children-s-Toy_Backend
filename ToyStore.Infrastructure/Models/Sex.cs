using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Sex
{
    public byte SexId { get; set; }

    public string SexName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ProductDetail> ProductDetails { get; set; } = new List<ProductDetail>();
}
