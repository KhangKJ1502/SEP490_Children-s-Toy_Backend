namespace ToyStore.Application.DTOs.Categories;

public class UpdateCategoryDto
{
    public short SuperCategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string? Status { get; set; }
}