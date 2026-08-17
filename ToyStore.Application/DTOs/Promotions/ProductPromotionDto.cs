namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// Data Transfer Object (DTO) chứa thông tin sản phẩm tham gia chương trình khuyến mãi giảm giá trực tiếp (DISCOUNT).
/// </summary>
public class ProductPromotionDto
{
    /// <summary>
    /// Mã ID sản phẩm.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Tên sản phẩm hiển thị.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Giá niêm yết gốc ban đầu của sản phẩm (VNĐ).
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Giá bán khuyến mãi sau khi giảm (VNĐ).
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Tỷ lệ phần trăm giảm giá (%).
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Số lượng tồn kho hiện tại của sản phẩm.
    /// </summary>
    public int Stock { get; set; }
}
