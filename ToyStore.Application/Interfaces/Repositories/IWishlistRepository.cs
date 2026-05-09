using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IWishlistRepository
{
    Task<Wishlist?> GetByAccountAndProductAsync(
        int accountId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<List<Wishlist>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);

    Task AddAsync(Wishlist wishlist, CancellationToken cancellationToken = default);

    void Remove(Wishlist wishlist);
}
