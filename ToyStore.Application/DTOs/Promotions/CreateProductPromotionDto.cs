namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// Data Transfer Object (DTO) chứa dữ liệu gán sản phẩm vào chương trình giảm giá trực tiếp (DISCOUNT).
/// </summary>
public class CreateProductPromotionDto
{
    /// <summary>
    /// Mã ID sản phẩm áp dụng giảm giá.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Giá bán sau khi đã giảm giá khuyến mãi (VNĐ).
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Tỷ lệ phần trăm giảm giá tương ứng (tùy chọn, từ 1% đến 99%).
    /// </summary>
    public decimal? DiscountPercent { get; set; }
}
