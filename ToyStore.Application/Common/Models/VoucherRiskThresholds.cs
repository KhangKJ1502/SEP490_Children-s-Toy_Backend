namespace ToyStore.Application.Common.Models;

/// <summary>
/// Cấu hình các ngưỡng rủi ro cho Voucher để tự động quyết định luồng phê duyệt (Approval Workflow) khi Staff tạo hoặc sửa voucher.
/// </summary>
public class VoucherRiskThresholds
{
    /// <summary>
    /// Mức trần giảm giá tối đa cho một đơn hàng (mặc định: 200,000 VNĐ).
    /// Nếu mức giảm của voucher vượt quá ngưỡng này, voucher do Staff tạo sẽ bị chuyển sang trạng thái Pending chờ Admin duyệt.
    /// </summary>
    public decimal MaxDiscountCap { get; set; } = 200_000m;

    /// <summary>
    /// Tổng ngân sách giảm giá dự kiến tối đa cho một chiến dịch voucher (mặc định: 20,000,000 VNĐ).
    /// Được tính bằng: MaxDiscountCap (hoặc DiscountValue) * TotalQuantity.
    /// Nếu vượt quá ngưỡng này, voucher sẽ cần Admin phê duyệt.
    /// </summary>
    public decimal MaxTotalDiscount { get; set; } = 20_000_000m;
}
