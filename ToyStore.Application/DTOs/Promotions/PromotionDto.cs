namespace ToyStore.Application.DTOs.Promotions;

public class PromotionDto
{
    public int PromotionId { get; set; }

    public int CreatedBy { get; set; }

    public string PromotionName { get; set; } = string.Empty;

    public string PromotionType { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public int Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
