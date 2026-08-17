namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// Data Transfer Object (DTO) phục vụ cập nhật chương trình khuyến mãi (hỗ trợ Partial Update, tất cả các trường đều nullable).
/// </summary>
public class UpdatePromotionDto
{
    /// <summary>
    /// Tên mới của chương trình khuyến mãi.
    /// </summary>
    public string? PromotionName { get; set; }

    /// <summary>
    /// Loại hình khuyến mãi mới ("DISCOUNT" hoặc "FLASH_SALE").
    /// </summary>
    public string? PromotionType { get; set; }

    /// <summary>
    /// Mô tả chi tiết mới của chương trình.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Thời điểm bắt đầu mới (UTC).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Thời điểm kết thúc mới (UTC).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Trạng thái mới của chương trình ("Scheduled", "Active", "Inactive", "Expired").
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Độ ưu tiên mới của chương trình.
    /// </summary>
    public int? Priority { get; set; }

    /// <summary>
    /// Đánh dấu xóa mềm chương trình khuyến mãi (true).
    /// </summary>
    public bool? IsDeleted { get; set; }

    /// <summary>
    /// Danh sách sản phẩm giảm giá mới (nếu gửi null thì giữ nguyên, nếu gửi mảng thì đồng bộ danh sách).
    /// </summary>
    public List<CreateProductPromotionDto>? ProductPromotions { get; set; }

    /// <summary>
    /// Danh sách các khung giờ Flash Sale mới (nếu gửi null thì giữ nguyên, nếu gửi mảng thì đồng bộ danh sách).
    /// </summary>
    public List<CreatePromotionTimeSlotDto>? PromotionTimeSlots { get; set; }
}
