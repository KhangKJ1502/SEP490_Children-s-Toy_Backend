namespace ToyStore.Domain.Constants;

/// <summary>
/// Các hằng số giá trị nội dung lý do hoàn tiền (OrderRefundReasons) được hệ thống dùng để tra cứu (khớp với DB seed).
/// </summary>
public static class RefundReasons
{
    /// <summary>Lý do giao hàng thất bại / không thể giao được hàng từ phía đơn vị vận chuyển GHN.</summary>
    public const string DeliveryFailedGhn = "Delivery failed / unable to deliver";

    /// <summary>Lý do hủy đơn hàng trước khi giao.</summary>
    public const string OrderCancelled = "Order cancelled before delivery";
}
