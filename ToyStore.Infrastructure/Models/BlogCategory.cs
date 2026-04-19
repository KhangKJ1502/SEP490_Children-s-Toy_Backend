using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class BlogCategory
{
    public short BlogCategoryId { get; set; }

    public string BlogCategoriesName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
}
