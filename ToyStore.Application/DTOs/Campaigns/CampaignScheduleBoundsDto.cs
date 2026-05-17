namespace ToyStore.Application.DTOs.Campaigns;

/// <summary>
/// Khung giờ gửi hợp lệ (UTC) giao giữa rule hệ thống và đối tượng gắn (voucher/sale/product),
/// dùng để hiển thị trên form lên lịch admin.
/// </summary>
public class CampaignScheduleBoundsDto
{
    public DateTime EarliestUtc { get; set; }

    public DateTime LatestUtc { get; set; }

    /// <summary>EarliestUtc &lt;= LatestUtc — nếu false, không còn mốc nào thỏa cùng lúc.</summary>
    public bool IsFeasible { get; set; }

    public string? ReferenceType { get; set; }

    /// <summary>SALE: FLASH_SALE, DISCOUNT, … khi đọc được từ promotion.</summary>
    public string? PromotionType { get; set; }

    /// <summary>Đã áp dụng ràng buộc từ voucher/sale/product (khác chỉ min 30p / max 90 ngày).</summary>
    public bool ReferenceRulesApplied { get; set; }

    /// <summary>Khi không đọc được reference hoặc entity không active — vẫn trả khung chung; kèm cảnh báo.</summary>
    public string? ReferenceHintWarning { get; set; }
}
