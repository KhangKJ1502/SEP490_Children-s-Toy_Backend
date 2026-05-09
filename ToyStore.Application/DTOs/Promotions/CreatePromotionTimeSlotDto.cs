namespace ToyStore.Application.DTOs.Promotions;

public class CreatePromotionTimeSlotDto
{
    /// <summary>Thời điểm bắt đầu slot — phải gửi theo UTC ISO 8601.</summary>
    public DateTime StartAt { get; set; }

    /// <summary>Thời điểm kết thúc slot — phải gửi theo UTC ISO 8601.</summary>
    public DateTime EndAt { get; set; }

    public string Status { get; set; } = "Scheduled";

    /// <summary>Sản phẩm tham gia slot này (bắt buộc với FLASH_SALE).</summary>
    public List<CreatePromotionProductSlotDto> PromotionProductSlots { get; set; } = new();
}
