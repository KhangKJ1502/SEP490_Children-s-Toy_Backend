namespace ToyStore.Application.DTOs.Categories;

public class CategoryListDto
{
    public short CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public short SuperCategoryId { get; set; }

    public string SuperCategoryName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}