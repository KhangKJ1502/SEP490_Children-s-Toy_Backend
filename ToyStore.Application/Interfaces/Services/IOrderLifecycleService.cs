using ToyStore.Application.Common.Models;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service quản lý vòng đời đơn hàng và các tác vụ liên quan (stock, refund, voucher).
/// </summary>
public interface IOrderLifecycleService
{
    /// <summary>
    /// Thực hiện hủy đơn hàng (internal logic).
    /// </summary>
    /// <param name="restoreCart">
    /// true  = khôi phục sản phẩm về giỏ hàng (dùng khi cancel từ trang thanh toán QR).
    /// false = KHÔNG khôi phục giỏ hàng (dùng khi cancel từ Order Detail / Order History).
    /// </param>
    Task<Result> CancelOrderInternalAsync(Order order, string reason, int cancelledByAccountId, bool restoreCart = false, bool restoreVoucher = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hoàn thành đơn hàng (internal logic).
    /// </summary>
    /// <param name="changedByAccountId">Người xác nhận (khách). null = hệ thống (auto-complete job).</param>
    /// <param name="enforceOwnerCheck">Có bắt buộc kiểm tra quyền sở hữu đơn hàng hay không (dùng cho customer).</param>
    Task<Result<Order>> CompleteOrderAsync(int orderId, int? changedByAccountId = null, bool enforceOwnerCheck = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh dấu đơn hàng là đã giao (internal logic).
    /// </summary>
    Task<Result> DeliverOrderAsync(int orderId, CancellationToken cancellationToken = default);
}
