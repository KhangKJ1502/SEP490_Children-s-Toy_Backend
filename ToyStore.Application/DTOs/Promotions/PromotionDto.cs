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

    /// <summary>Sản phẩm tham gia promotion DISCOUNT (không phân theo slot).</summary>
    public List<ProductPromotionDto> ProductPromotions { get; set; } = new();

    /// <summary>Các khung giờ flash-sale (chỉ cho FLASH_SALE). Mỗi slot chứa danh sách sản phẩm riêng.</summary>
    public List<PromotionTimeSlotDto> PromotionTimeSlots { get; set; } = new();
}
