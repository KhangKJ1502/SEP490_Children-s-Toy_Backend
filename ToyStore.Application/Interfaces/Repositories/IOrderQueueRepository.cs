using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IOrderQueueRepository
{
    Task<List<OrderQueue>> GetPendingAsync(CancellationToken cancellationToken = default);

    Task<OrderQueue?> GetByIdAsync(int queueId, CancellationToken cancellationToken = default);

    Task<OrderQueue?> GetOldestPendingAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsPendingForOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<OrderQueue> CreateAsync(OrderQueue entry, CancellationToken cancellationToken = default);

    Task MarkResolvedAsync(int queueId, int assignedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả OrderQueue được tạo trong khoảng [fromUtc, toUtc] để dùng cho digest email.
    /// </summary>
    /// <summary>
    /// Tự động đánh dấu IsResolved = true cho tất cả hàng đợi của một đơn hàng khi đơn hàng hoàn thành/giao xong/bị hủy.
    /// </summary>
    Task ResolveByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
}


