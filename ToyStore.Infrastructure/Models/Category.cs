using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Category
{
    public short CategoryId { get; set; }

    public short SuperCategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual SuperCategory SuperCategory { get; set; } = null!;
}
