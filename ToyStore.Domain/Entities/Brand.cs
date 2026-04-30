using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class Brand
{
    public short BrandId { get; set; }

    public string BrandName { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
