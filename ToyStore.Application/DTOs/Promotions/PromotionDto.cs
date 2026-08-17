namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// Data Transfer Object (DTO) chứa toàn bộ thông tin chi tiết của một chương trình khuyến mãi để trả về cho Client.
/// </summary>
public class PromotionDto
{
    /// <summary>
    /// Mã ID định danh của chương trình khuyến mãi.
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// ID tài khoản nhân viên/admin đã tạo chương trình này.
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// Tên chương trình khuyến mãi.
    /// </summary>
    public string PromotionName { get; set; } = string.Empty;

    /// <summary>
    /// Loại hình khuyến mãi: "DISCOUNT" hoặc "FLASH_SALE".
    /// </summary>
    public string PromotionType { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết chương trình.
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
    /// Trạng thái hiện tại ("Scheduled", "Active", "Inactive", "Expired").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Độ ưu tiên áp dụng của chương trình.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Thời điểm tạo bản ghi (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm cập nhật bản ghi gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Danh sách các sản phẩm tham gia khuyến mãi thông thường (DISCOUNT).
    /// </summary>
    public List<ProductPromotionDto> ProductPromotions { get; set; } = new();

    /// <summary>
    /// Danh sách các khung giờ Flash Sale (FLASH_SALE), mỗi khung giờ chứa danh sách sản phẩm riêng.
    /// </summary>
    public List<PromotionTimeSlotDto> PromotionTimeSlots { get; set; } = new();
}
