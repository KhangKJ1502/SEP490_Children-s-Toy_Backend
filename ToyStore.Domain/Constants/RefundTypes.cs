namespace ToyStore.Domain.Constants;

/// <summary>
/// Phân loại hình thức hoàn tiền.
/// </summary>
public static class RefundTypes
{
    /// <summary>Trả hàng vật lý và nhận hoàn tiền.</summary>
    public const string ReturnAndRefund = "ReturnAndRefund";

    /// <summary>Chỉ hoàn tiền trực tiếp, không cần trả hàng vật lý.</summary>
    public const string RefundOnly = "RefundOnly";
}
