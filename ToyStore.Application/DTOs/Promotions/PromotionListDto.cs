namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// Data Transfer Object (DTO) dạng rút gọn hiển thị trong danh sách khuyến mãi phân trang.
/// </summary>
public class PromotionListDto
{
    /// <summary>
    /// Mã ID chương trình khuyến mãi.
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// Tên chương trình khuyến mãi.
    /// </summary>
    public string PromotionName { get; set; } = string.Empty;

    /// <summary>
    /// Loại khuyến mãi ("DISCOUNT" hoặc "FLASH_SALE").
    /// </summary>
    public string PromotionType { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm bắt đầu chương trình (UTC).
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Thời điểm kết thúc chương trình (UTC).
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Trạng thái chương trình ("Scheduled", "Active", "Inactive", "Expired").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Độ ưu tiên áp dụng.
    /// </summary>
    public int Priority { get; set; }
}
