namespace ToyStore.Application.DTOs.Promotions;

public class CreatePromotionDto
{
    public string PromotionName { get; set; } = string.Empty;

    public string PromotionType { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public int Priority { get; set; }
}
