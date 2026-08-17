namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// Data Transfer Object (DTO) chứa dữ liệu chi tiết của sản phẩm tham gia một khung giờ Flash Sale (PromotionProductSlot).
/// </summary>
public class PromotionProductSlotDto
{
    /// <summary>
    /// Mã ID định danh của bản ghi gán sản phẩm vào khung giờ.
    /// </summary>
    public int SlotProductId { get; set; }

    /// <summary>
    /// Mã ID của khung giờ Flash Sale.
    /// </summary>
    public int TimeSlotId { get; set; }

    /// <summary>
    /// Mã ID sản phẩm.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Tên sản phẩm.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn URL ảnh chính của sản phẩm (dùng để hiển thị trong banner/lưới Flash Sale).
    /// </summary>
    public string? MainImageUrl { get; set; }

    /// <summary>
    /// Giá gốc ban đầu của sản phẩm (VNĐ).
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Giá bán Flash Sale trong khung giờ này (VNĐ).
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Tỷ lệ phần trăm giảm giá (%).
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Số lượng sản phẩm tối đa mở bán trong khung giờ này.
    /// </summary>
    public int SaleQuantity { get; set; }

    /// <summary>
    /// Số lượng sản phẩm đã bán thành công trong khung giờ này.
    /// </summary>
    public int SoldQuantity { get; set; }

    /// <summary>
    /// Số lượng sản phẩm đang được giữ chỗ trong các đơn hàng chờ thanh toán.
    /// </summary>
    public int ReservedQuantity { get; set; }
}
