namespace ToyStore.Application.DTOs.Categories;

public class CreateCategoryDto
{
    public short SuperCategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;
}