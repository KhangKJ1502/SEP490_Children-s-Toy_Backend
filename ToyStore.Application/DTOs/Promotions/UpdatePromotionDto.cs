namespace ToyStore.Application.DTOs.Promotions;

public class UpdatePromotionDto
{
    public string? PromotionName { get; set; }

    public string? PromotionType { get; set; }

    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Status { get; set; }

    public int? Priority { get; set; }

    public bool? IsDeleted { get; set; }

    public List<CreateProductPromotionDto>? ProductPromotions { get; set; }
}
