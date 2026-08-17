using System;

namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// Data Transfer Object (DTO) chứa thông tin chương trình khuyến mãi (cả DISCOUNT và FLASH_SALE) đang áp dụng trực tiếp cho một sản phẩm cụ thể.
/// </summary>
public class ProductPromotionInfoDto
{
    /// <summary>
    /// Mã ID của chương trình khuyến mãi.
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// Tên chương trình khuyến mãi.
    /// </summary>
    public string PromotionName { get; set; } = string.Empty;

    /// <summary>
    /// Loại hình khuyến mãi: "DISCOUNT" hoặc "FLASH_SALE".
    /// </summary>
    public string PromotionType { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm bắt đầu có hiệu lực (hoặc StartAt của khung giờ Flash Sale) - UTC.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Thời điểm kết thúc hiệu lực (hoặc EndAt của khung giờ Flash Sale) - UTC.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Trạng thái của khuyến mãi hoặc khung giờ ("Scheduled", "Active", "Inactive", "Expired").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Giá bán sau khuyến mãi của sản phẩm (VNĐ).
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Tỷ lệ phần trăm giảm giá (%).
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Số lượng sản phẩm mở bán khuyến mãi (chỉ áp dụng cho FLASH_SALE, null nếu là DISCOUNT).
    /// </summary>
    public int? SaleQuantity { get; set; }

    /// <summary>
    /// Số lượng sản phẩm đã bán trong khuyến mãi (chỉ áp dụng cho FLASH_SALE, null nếu là DISCOUNT).
    /// </summary>
    public int? SoldQuantity { get; set; }

    /// <summary>
    /// Độ ưu tiên áp dụng của chương trình khuyến mãi.
    /// </summary>
    public int Priority { get; set; }
}
