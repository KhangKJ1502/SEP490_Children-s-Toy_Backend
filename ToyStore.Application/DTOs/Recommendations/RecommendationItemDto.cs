namespace ToyStore.Application.DTOs.Recommendations;

/// <summary>
/// 1 item trong danh sách gợi ý — bao gồm thông tin product gọn nhẹ để FE render nhanh.
/// </summary>
public class RecommendationItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public int? DiscountPercent { get; set; }
    public string? PromotionType { get; set; }
    public int Quantity { get; set; }
    public string ProductStatus { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? BrandId { get; set; }
    public string? BrandName { get; set; }
    public string? MainImageUrl { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int SoldQuantity { get; set; }

    /// <summary>Điểm gợi ý đã tính ra (debug/UI ngầm — không hiển thị cho user).</summary>
    public decimal Score { get; set; }

    /// <summary>Câu lý do hiển thị: "Vì bạn đã xem...", "Khách hàng cùng mua...", "Đang trending".</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Mã lý do dùng cho analytics (không hiển thị user).</summary>
    public string ReasonCode { get; set; } = string.Empty;
}
