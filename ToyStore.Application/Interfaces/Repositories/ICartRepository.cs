using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface ICartRepository
{
    Task<Cart?> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);

    Task<Cart?> GetByAccountIdWithItemsAsync(int accountId, CancellationToken cancellationToken = default);
    Task<Cart?> GetByAccountIdWithRemovedItemsAsync(int accountId, CancellationToken cancellationToken = default);

    Task<Cart> CreateAsync(int accountId, CancellationToken cancellationToken = default);

    Task<CartItem?> GetItemByIdForAccountAsync(int accountId, int cartItemId, CancellationToken cancellationToken = default);

    Task<CartItem?> GetItemByProductAsync(int cartId, int productId, CancellationToken cancellationToken = default);

    Task<List<int>> GetAccountIdsByProductIdAsync(int productId, CancellationToken cancellationToken = default);

    Task<List<int>> GetAccountIdsByProductIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default);

    void AddItem(CartItem cartItem);

    void UpdateItem(CartItem cartItem);

    void RemoveItem(CartItem cartItem);

    void UpdateCart(Cart cart);
}
