using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly SEP490ToyStoreContext _context;

    public WishlistRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public Task<Wishlist?> GetByAccountAndProductAsync(
        int accountId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        return _context.Wishlists.FirstOrDefaultAsync(
            x => x.AccountId == accountId && x.ProductId == productId,
            cancellationToken);
    }

    public Task<List<Wishlist>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return _context.Wishlists
            .AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Wishlist>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _context.Wishlists
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Wishlist wishlist, CancellationToken cancellationToken = default)
    {
        return _context.Wishlists.AddAsync(wishlist, cancellationToken).AsTask();
    }

    public void Remove(Wishlist wishlist)
    {
        _context.Wishlists.Remove(wishlist);
    }
}
