namespace ToyStore.Application.Validators.Vouchers;

/// <summary>
/// Lớp chứa các hằng số và tập hợp giá trị hợp lệ dùng chung cho quá trình validation của Voucher.
/// </summary>
internal static class VoucherValidatorConstants
{
    /// <summary>
    /// Danh sách các loại hình giảm giá được hỗ trợ trong hệ thống:
    /// - FIXED: Giảm số tiền cố định (VNĐ).
    /// - PERCENTAGE: Giảm theo tỷ lệ phần trăm (%).
    /// </summary>
    internal static readonly HashSet<string> AllowedDiscountTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "FIXED",
        "PERCENTAGE"
    };

    /// <summary>
    /// Danh sách các đối tượng áp dụng mã giảm giá:
    /// - ORDER_TOTAL: Áp dụng trên tổng tiền hàng (trước phí ship).
    /// - SHIPPING_FEE: Áp dụng giảm trực tiếp trên phí vận chuyển.
    /// - FINAL_PRICE: Áp dụng trên tổng giá trị thanh toán cuối cùng (cần phê duyệt nghiêm ngặt).
    /// </summary>
    internal static readonly HashSet<string> AllowedDiscountTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "ORDER_TOTAL",
        "SHIPPING_FEE",
        "FINAL_PRICE"
    };

    /// <summary>
    /// Danh sách tất cả các trạng thái hợp lệ của Voucher trong hệ thống.
    /// </summary>
    internal static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        ToyStore.Domain.Constants.VoucherStatuses.Scheduled,
        ToyStore.Domain.Constants.VoucherStatuses.Active,
        ToyStore.Domain.Constants.VoucherStatuses.Inactive,
        ToyStore.Domain.Constants.VoucherStatuses.Expired,
        ToyStore.Domain.Constants.VoucherStatuses.Pending,
        ToyStore.Domain.Constants.VoucherStatuses.Rejected
    };
}
