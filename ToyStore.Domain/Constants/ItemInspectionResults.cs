namespace ToyStore.Domain.Constants;

/// <summary>
/// Các kết quả kiểm tra chất lượng cho từng sản phẩm hoàn trả.
/// </summary>
public static class ItemInspectionResults
{
    /// <summary>Sản phẩm đạt chất lượng, nguyên vẹn, được nhập kho.</summary>
    public const string Passed = "Passed";

    /// <summary>Sản phẩm bị hư hỏng do lỗi từ phía khách hàng.</summary>
    public const string FailedCustomerFault = "FailedCustomerFault";

    /// <summary>Sản phẩm bị hư hỏng do lỗi từ phía đơn vị vận chuyển.</summary>
    public const string FailedCarrierFault = "FailedCarrierFault";
}
