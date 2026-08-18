namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// Data Transfer Object (DTO) chứa dữ liệu tạo mới một khung giờ Flash Sale (PromotionTimeSlot).
/// </summary>
public class CreatePromotionTimeSlotDto
{
    /// <summary>
    /// Thời điểm bắt đầu khung giờ Flash Sale (UTC ISO 8601).
    /// </summary>
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Thời điểm kết thúc khung giờ Flash Sale (UTC ISO 8601).
    /// </summary>
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Trạng thái của khung giờ: "Scheduled" (Mặc định), "Active", hoặc "Expired".
    /// </summary>
    public string Status { get; set; } = "Scheduled";

    /// <summary>
    /// Danh sách các sản phẩm và số lượng/giá bán tham gia trong khung giờ này.
    /// </summary>
    public List<CreatePromotionProductSlotDto> PromotionProductSlots { get; set; } = new();
}
