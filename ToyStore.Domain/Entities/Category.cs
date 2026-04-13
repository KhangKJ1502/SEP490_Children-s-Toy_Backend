namespace ToyStore.Domain.Entities;

/// <summary>
/// Represents a product category.
/// </summary>
public class Category : AuditableEntity
{
    /// <summary>
    /// Category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Category description.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// URL-friendly slug for the category.
    /// </summary>
    public string Slug { get; set; } = string.Empty;
    
    /// <summary>
    /// Category image URL.
    /// </summary>
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// Parent category ID for hierarchical categories.
    /// </summary>
    public Guid? ParentCategoryId { get; set; }
    
    /// <summary>
    /// Parent category for hierarchical structure.
    /// </summary>
    public virtual Category? ParentCategory { get; set; }
    
    /// <summary>
    /// Child categories.
    /// </summary>
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    
    /// <summary>
    /// Products in this category.
    /// </summary>
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    
    /// <summary>
    /// Display order for sorting.
    /// </summary>
    public int DisplayOrder { get; set; }
    
    /// <summary>
    /// Whether the category is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
