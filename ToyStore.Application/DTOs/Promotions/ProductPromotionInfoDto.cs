using System;

namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// DTO chứa thông tin promotion đang áp dụng cho một sản phẩm.
/// </summary>
public class ProductPromotionInfoDto
{
    public int PromotionId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public string PromotionType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public int? SaleQuantity { get; set; }
    public int? SoldQuantity { get; set; }
    public int Priority { get; set; }
}
