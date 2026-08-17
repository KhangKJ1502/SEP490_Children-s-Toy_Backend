namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// Data Transfer Object (DTO) chứa dữ liệu gán sản phẩm vào một khung giờ Flash Sale (PromotionProductSlot).
/// </summary>
public class CreatePromotionProductSlotDto
{
    /// <summary>
    /// Mã ID của sản phẩm tham gia Flash Sale.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Giá bán Flash Sale trong khung giờ này (VNĐ).
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Tỷ lệ phần trăm giảm giá (nếu không truyền, hệ thống sẽ tự động tính dựa trên giá gốc và giá bán Flash Sale).
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Số lượng sản phẩm tối đa được phép bán với giá Flash Sale trong khung giờ này.
    /// </summary>
    public int SaleQuantity { get; set; }
}
