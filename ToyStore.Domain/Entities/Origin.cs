using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class Origin
{
    public byte OriginId { get; set; }

    public string OriginName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ProductDetail> ProductDetails { get; set; } = new List<ProductDetail>();
}
