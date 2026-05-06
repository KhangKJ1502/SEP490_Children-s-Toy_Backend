namespace ToyStore.Application.DTOs.SuperCategories;

public class UpdateSuperCategoryDto
{
    public string SuperCategoryName { get; set; } = string.Empty;

    public string? Status { get; set; }
}