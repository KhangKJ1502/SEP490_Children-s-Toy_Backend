namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// Data Transfer Object (DTO) chứa thông tin chi tiết một khung giờ Flash Sale (PromotionTimeSlot) trả về cho Client.
/// </summary>
public class PromotionTimeSlotDto
{
    /// <summary>
    /// Mã ID định danh của khung giờ.
    /// </summary>
    public int TimeSlotId { get; set; }

    /// <summary>
    /// Mã ID của chương trình khuyến mãi Flash Sale chứa khung giờ này.
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// Thời điểm bắt đầu khung giờ (UTC).
    /// </summary>
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Thời điểm kết thúc khung giờ (UTC).
    /// </summary>
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Trạng thái của khung giờ ("Scheduled", "Active", "Inactive", "Expired").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách các sản phẩm và số lượng mở bán trong khung giờ này.
    /// </summary>
    public List<PromotionProductSlotDto> PromotionProductSlots { get; set; } = new();
}
