namespace ToyStore.Domain.Constants;

/// <summary>
/// Xác định bên chịu phí vận chuyển hoàn trả (customer → shop).
/// </summary>
public static class RefundResponsibleParty
{
    /// <summary>Cửa hàng chịu phí vận chuyển hoàn trả.</summary>
    public const string Store = "Store";

    /// <summary>Khách hàng chịu phí vận chuyển hoàn trả (bị trừ vào FinalRefundAmount).</summary>
    public const string Customer = "Customer";
}

/// <summary>
/// Xác định nguyên nhân hàng hóa bị hư hỏng trong quá trình vận chuyển hoàn trả.
/// Được Merchandise đề xuất → Staff/Admin xác nhận.
/// </summary>
public static class RefundDamageResponsibility
{
    /// <summary>Hàng hỏng do khách hàng gửi sai tình trạng.</summary>
    public const string Customer = "Customer";

    /// <summary>Hàng hỏng do đơn vị vận chuyển (GHN) trong quá trình vận chuyển.</summary>
    public const string Carrier = "Carrier";
}
