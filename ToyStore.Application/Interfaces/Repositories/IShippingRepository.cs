using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IShippingRepository
{
    Task<ShippingProviderTransaction?> GetTransactionByOrderIdAsync(int orderId, CancellationToken ct = default);
}
