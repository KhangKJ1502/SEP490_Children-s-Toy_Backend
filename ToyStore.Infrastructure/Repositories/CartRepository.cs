using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly SEP490ToyStoreContext _context;

    public CartRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public Task<Cart?> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return _context.Carts.FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
    }

    public Task<Cart?> GetByAccountIdWithItemsAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return _context.Carts
            .Include(x => x.CartItems.Where(ci => ci.RemovedAt == null))
                .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.ProductImage)
            .Include(x => x.CartItems.Where(ci => ci.RemovedAt == null))
                .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.PromotionProductSlots)
                        .ThenInclude(pps => pps.TimeSlot)
                            .ThenInclude(ts => ts.Promotion)
            .Include(x => x.CartItems.Where(ci => ci.RemovedAt == null))
                .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.ProductPromotions)
                        .ThenInclude(pp => pp.Promotion)
                            .ThenInclude(p => p.PromotionTimeSlots)
            .FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
    }

    public Task<Cart?> GetByAccountIdWithRemovedItemsAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return _context.Carts
            .Include(x => x.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
    }

    public async Task<Cart> CreateAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var cart = new Cart
        {
            AccountId = accountId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Carts.AddAsync(cart, cancellationToken);
        return cart;
    }

    public Task<CartItem?> GetItemByIdForAccountAsync(int accountId, int cartItemId, CancellationToken cancellationToken = default)
    {
        return _context.CartItems
            .Include(x => x.Product)
                .ThenInclude(p => p.ProductImage)
            .Include(x => x.Product)
                .ThenInclude(p => p.PromotionProductSlots)
                    .ThenInclude(pps => pps.TimeSlot)
                        .ThenInclude(ts => ts.Promotion)
            .Include(x => x.Product)
                .ThenInclude(p => p.ProductPromotions)
                    .ThenInclude(pp => pp.Promotion)
                        .ThenInclude(p => p.PromotionTimeSlots)
            .FirstOrDefaultAsync(
                x => x.CartItemId == cartItemId
                     && x.RemovedAt == null
                     && x.Cart.AccountId == accountId,
                cancellationToken);
    }

    public Task<CartItem?> GetItemByProductAsync(int cartId, int productId, CancellationToken cancellationToken = default)
    {
        return _context.CartItems
            .Include(x => x.Product)
                .ThenInclude(p => p.ProductImage)
            .Include(x => x.Product)
                .ThenInclude(p => p.PromotionProductSlots)
                    .ThenInclude(pps => pps.TimeSlot)
                        .ThenInclude(ts => ts.Promotion)
            .Include(x => x.Product)
                .ThenInclude(p => p.ProductPromotions)
                    .ThenInclude(pp => pp.Promotion)
                        .ThenInclude(p => p.PromotionTimeSlots)
            .FirstOrDefaultAsync(x => x.CartId == cartId && x.ProductId == productId, cancellationToken);
    }

    public Task<List<int>> GetAccountIdsByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _context.CartItems
            .AsNoTracking()
            .Where(x => x.ProductId == productId && x.RemovedAt == null)
            .Select(x => x.Cart.AccountId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task<List<int>> GetAccountIdsByProductIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return Task.FromResult(new List<int>());
        }

        return _context.CartItems
            .AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId) && x.RemovedAt == null)
            .Select(x => x.Cart.AccountId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public void AddItem(CartItem cartItem)
    {
        _context.CartItems.Add(cartItem);
    }

    public void UpdateItem(CartItem cartItem)
    {
        _context.CartItems.Update(cartItem);
    }

    public void RemoveItem(CartItem cartItem)
    {
        _context.CartItems.Remove(cartItem);
    }

    public void RemoveItems(IEnumerable<CartItem> cartItems)
    {
        _context.CartItems.RemoveRange(cartItems);
    }

    public void UpdateCart(Cart cart)
    {
        _context.Carts.Update(cart);
    }
}
