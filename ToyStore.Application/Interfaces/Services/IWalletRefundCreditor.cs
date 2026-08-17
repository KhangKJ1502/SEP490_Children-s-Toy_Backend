namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Interface dịch vụ cộng tiền hoàn trả vào Ví nội bộ (Customer Wallet) của khách hàng.
/// Tự động khởi tạo ví mới nếu khách hàng chưa có ví.
/// </summary>
public interface IWalletRefundCreditor
{
    /// <summary>
    /// Cộng tiền hoàn vào số dư ví của khách hàng với tính chất lũy thừa Idempotency (tránh cộng tiền trùng lặp):
    /// - Kiểm tra hoặc sinh idempotencyKey theo định dạng REFUND_{orderCode}.
    /// - Cộng số dư ví khả dụng (Balance).
    /// - Tạo bản ghi giao dịch ví WalletTransaction với loại REFUND.
    /// </summary>
    /// <param name="accountId">Mã ID tài khoản khách hàng.</param>
    /// <param name="amount">Số tiền cần hoàn vào ví (VNĐ).</param>
    /// <param name="orderCode">Mã code đơn hàng được hoàn tiền.</param>
    /// <param name="relatedOrderId">Mã ID đơn hàng liên quan (tùy chọn).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <param name="idempotencyKey">Khóa Idempotency chống trùng lặp giao dịch (tùy chọn).</param>
    /// <returns>true nếu cộng tiền thành công hoặc giao dịch đã hoàn tất trước đó; false nếu có lỗi.</returns>
    Task<bool> CreditRefundAsync(
        int accountId,
        decimal amount,
        string orderCode,
        int? relatedOrderId = null,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null);
}
