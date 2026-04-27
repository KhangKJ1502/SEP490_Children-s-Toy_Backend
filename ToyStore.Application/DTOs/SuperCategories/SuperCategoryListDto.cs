namespace ToyStore.Application.DTOs.SuperCategories;

public class SuperCategoryListDto
{
    public short SuperCategoryId { get; set; }

    public string SuperCategoryName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}