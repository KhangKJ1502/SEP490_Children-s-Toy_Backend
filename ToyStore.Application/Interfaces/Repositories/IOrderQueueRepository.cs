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
}
