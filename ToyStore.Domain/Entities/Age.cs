using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class Age
{
    public byte AgeId { get; set; }

    public string AgeRange { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ProductDetail> ProductDetails { get; set; } = new List<ProductDetail>();
}
