namespace ToyStore.Application.DTOs.Promotions;

public class PromotionListDto
{
    public int PromotionId { get; set; }

    public string PromotionName { get; set; } = string.Empty;

    public string PromotionType { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public int Priority { get; set; }
}
