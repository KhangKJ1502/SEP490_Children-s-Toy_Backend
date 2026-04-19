using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Material
{
    public short MaterialId { get; set; }

    public string MaterialName { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ProductDetail> ProductDetails { get; set; } = new List<ProductDetail>();
}
