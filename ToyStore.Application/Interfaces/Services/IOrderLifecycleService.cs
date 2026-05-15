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
    Task<Result> CancelOrderInternalAsync(Order order, string reason, int cancelledByAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hoàn thành đơn hàng (internal logic).
    /// </summary>
    Task<Result> CompleteOrderAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh dấu đơn hàng là đã giao (internal logic).
    /// </summary>
    Task<Result> DeliverOrderAsync(int orderId, CancellationToken cancellationToken = default);
}
