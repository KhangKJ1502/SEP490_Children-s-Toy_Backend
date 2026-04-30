using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class SuperCategory
{
    public short SuperCategoryId { get; set; }

    public string SuperCategoryName { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
}
