namespace ToyStore.Application.Common.Models;

public class SuperCategoryModel
{
    public short SuperCategoryId { get; set; }

    public string SuperCategoryName { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class CategoryModel
{
    public short CategoryId { get; set; }

    public short SuperCategoryId { get; set; }

    public string SuperCategoryName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class BrandModel
{
    public short BrandId { get; set; }

    public string BrandName { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
