using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class PriceRange
{
    public byte PriceRangeId { get; set; }

    public decimal PriceRangeMin { get; set; }

    public decimal PriceRangeMax { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
