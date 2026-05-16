namespace ToyStore.Application.Common.Models;

/// <summary>
/// Cấu hình các ngưỡng rủi ro cho Voucher để quyết định luồng duyệt.
/// </summary>
public class VoucherRiskThresholds
{
    public decimal MaxDiscountCap { get; set; } = 200_000m;
    public decimal MaxTotalDiscount { get; set; } = 20_000_000m;
}
