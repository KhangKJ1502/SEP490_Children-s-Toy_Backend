namespace ToyStore.Application.Validators.Vouchers;

/// <summary>
/// Hằng số dùng chung cho các validator của voucher.
/// </summary>
internal static class VoucherValidatorConstants
{
    internal static readonly HashSet<string> AllowedDiscountTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "FIXED",
        "PERCENTAGE"
    };

    internal static readonly HashSet<string> AllowedDiscountTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "ORDER_TOTAL",
        "SHIPPING_FEE"
    };

    internal static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Scheduled",
        "Active",
        "Inactive",
        "Expired"
    };
}
