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
    /// <param name="order">Đơn hàng cần hủy (nên include đầy đủ OrderDetails).</param>
    /// <param name="reason">Lý do hủy.</param>
    /// <param name="cancelledByAccountId">ID người thực hiện hủy.</param>
    Task<Result> CancelOrderInternalAsync(Order order, string reason, int cancelledByAccountId, CancellationToken cancellationToken = default);
}
