namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// Data Transfer Object (DTO) chứa dữ liệu đầu vào để tạo mới một chương trình khuyến mãi (Promotion).
/// </summary>
public class CreatePromotionDto
{
    /// <summary>
    /// Tên chương trình khuyến mãi (ví dụ: "Sale Quốc tế Thiếu nhi 1/6", "Flash Sale Giờ Vàng").
    /// </summary>
    public string PromotionName { get; set; } = string.Empty;

    /// <summary>
    /// Loại hình khuyến mãi: "DISCOUNT" (giảm giá sản phẩm thông thường) hoặc "FLASH_SALE" (Flash sale theo khung giờ).
    /// </summary>
    public string PromotionType { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết thể lệ, nội dung chương trình khuyến mãi.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Thời điểm bắt đầu chương trình (UTC).
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Thời điểm kết thúc chương trình (UTC).
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Trạng thái ban đầu của chương trình: "Scheduled" (Đã lên lịch), "Active" (Đang diễn ra), hoặc "Inactive" (Tạm dừng).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Độ ưu tiên áp dụng của chương trình khuyến mãi (số càng lớn độ ưu tiên càng cao).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Danh sách các sản phẩm áp dụng giảm giá trực tiếp (sử dụng khi PromotionType = "DISCOUNT").
    /// </summary>
    public List<CreateProductPromotionDto> ProductPromotions { get; set; } = new();

    /// <summary>
    /// Danh sách các khung giờ Flash Sale (sử dụng khi PromotionType = "FLASH_SALE").
    /// </summary>
    public List<CreatePromotionTimeSlotDto> PromotionTimeSlots { get; set; } = new();
}
