namespace ToyStore.Domain.Constants;

/// <summary>
/// Phân loại nguồn tạo refund.
/// <para>Customer: khách tự tạo sau đơn Completed (≤3 ngày) — được phép GHN pickup.</para>
/// <para>System: hệ thống tự tạo khi GHN returned hàng về kho — bỏ qua pickup, chỉ Approve → Complete.</para>
/// </summary>
public static class RefundSources
{
    /// <summary>Khách hàng tự tạo yêu cầu hoàn trả.</summary>
    public const string Customer = "Customer";

    /// <summary>Hệ thống tự tạo khi GHN trả hàng về kho (delivery failure).</summary>
    public const string System = "System";
}
