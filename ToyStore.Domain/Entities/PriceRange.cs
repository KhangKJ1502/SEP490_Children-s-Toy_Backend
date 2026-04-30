using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class PriceRange
{
    public byte PriceRangeId { get; set; }

    public decimal PriceRangeMin { get; set; }

    public decimal PriceRangeMax { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
