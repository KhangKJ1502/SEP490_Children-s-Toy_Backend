namespace ToyStore.Application.DTOs.Promotions;

public class PromotionTimeSlotDto
{
    public int TimeSlotId { get; set; }

    public int PromotionId { get; set; }

    /// <summary>Thời điểm bắt đầu slot — UTC.</summary>
    public DateTime StartAt { get; set; }

    /// <summary>Thời điểm kết thúc slot — UTC.</summary>
    public DateTime EndAt { get; set; }

    public string Status { get; set; } = string.Empty;

    /// <summary>Danh sách sản phẩm tham gia slot (FLASH_SALE only).</summary>
    public List<PromotionProductSlotDto> PromotionProductSlots { get; set; } = new();
}
