using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for UserBehavior operations.
/// </summary>
public interface IUserBehaviorRepository
{
    /// <summary>
    /// Gets a user behavior by ID.
    /// </summary>
    Task<UserBehavior?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all user behaviors.
    /// </summary>
    Task<IReadOnlyList<UserBehavior>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new user behavior.
    /// </summary>
    Task<UserBehavior> AddAsync(UserBehavior behavior, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing user behavior.
    /// </summary>
    void Update(UserBehavior behavior);
    
    /// <summary>
    /// Removes a user behavior.
    /// </summary>
    void Remove(UserBehavior behavior);
    
    /// <summary>
    /// Gets behaviors by user ID.
    /// </summary>
    Task<IReadOnlyList<UserBehavior>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets behaviors by product ID.
    /// </summary>
    Task<IReadOnlyList<UserBehavior>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets behaviors by type.
    /// </summary>
    Task<IReadOnlyList<UserBehavior>> GetByBehaviorTypeAsync(BehaviorType type, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets recent behaviors for a user.
    /// </summary>
    Task<IReadOnlyList<UserBehavior>> GetRecentByUserAsync(
        Guid userId, 
        int limit = 50, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets most viewed products.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetMostViewedProductIdsAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets most purchased products.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetMostPurchasedProductIdsAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets recent behaviors for recommendation.
    /// </summary>
    Task<IReadOnlyList<UserBehavior>> GetRecentBehaviorsAsync(
        Guid userId,
        int limit = 100,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets behaviors by user ID and type.
    /// </summary>
    Task<IReadOnlyList<UserBehavior>> GetByTypeAsync(
        Guid userId,
        BehaviorType type,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets most viewed products within a date range.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetMostViewedProductsAsync(
        int limit,
        DateTime since,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets most purchased products within a date range.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetMostPurchasedProductsAsync(
        int limit,
        DateTime since,
        CancellationToken cancellationToken = default);
}
