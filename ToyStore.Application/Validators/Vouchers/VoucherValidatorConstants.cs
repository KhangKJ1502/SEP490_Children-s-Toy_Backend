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
        "SHIPPING_FEE",
        "FINAL_PRICE"
    };

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
