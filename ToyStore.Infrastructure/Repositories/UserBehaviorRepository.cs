using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// UserBehavior repository implementation.
/// </summary>
public class UserBehaviorRepository : IUserBehaviorRepository
{
    private readonly ToyStoreDbContext _context;
    private readonly DbSet<UserBehavior> _dbSet;
    
    public UserBehaviorRepository(ToyStoreDbContext context)
    {
        _context = context;
        _dbSet = context.Set<UserBehavior>();
    }
    
    public async Task<UserBehavior?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }
    
    public async Task<IReadOnlyList<UserBehavior>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }
    
    public async Task<UserBehavior> AddAsync(UserBehavior behavior, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(behavior, cancellationToken);
        return behavior;
    }
    
    public void Update(UserBehavior behavior)
    {
        _dbSet.Update(behavior);
    }
    
    public void Remove(UserBehavior behavior)
    {
        _dbSet.Remove(behavior);
    }
    
    public async Task<IReadOnlyList<UserBehavior>> GetByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.OccurredAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<UserBehavior>> GetByProductIdAsync(
        Guid productId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.ProductId == productId)
            .OrderByDescending(b => b.OccurredAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<UserBehavior>> GetByBehaviorTypeAsync(
        BehaviorType type, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.BehaviorType == type)
            .OrderByDescending(b => b.OccurredAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<UserBehavior>> GetRecentByUserAsync(
        Guid userId, 
        int limit = 50, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.OccurredAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Guid>> GetMostViewedProductIdsAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.BehaviorType == BehaviorType.View)
            .GroupBy(b => b.ProductId)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Guid>> GetMostPurchasedProductIdsAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.BehaviorType == BehaviorType.Purchase)
            .GroupBy(b => b.ProductId)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<UserBehavior>> GetRecentBehaviorsAsync(
        Guid userId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.Product)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.OccurredAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<UserBehavior>> GetByTypeAsync(
        Guid userId,
        BehaviorType type,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.UserId == userId && b.BehaviorType == type)
            .OrderByDescending(b => b.OccurredAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Guid>> GetMostViewedProductsAsync(
        int limit,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.BehaviorType == BehaviorType.View && b.OccurredAt >= since)
            .GroupBy(b => b.ProductId)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Guid>> GetMostPurchasedProductsAsync(
        int limit,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.BehaviorType == BehaviorType.Purchase && b.OccurredAt >= since)
            .GroupBy(b => b.ProductId)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);
    }
}
