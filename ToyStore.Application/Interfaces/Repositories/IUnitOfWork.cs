namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Unit of Work pattern for managing transactions across repositories.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Product repository instance.
    /// </summary>
    IProductRepository Products { get; }
    
    /// <summary>
    /// Order repository instance.
    /// </summary>
    IOrderRepository Orders { get; }
    
    /// <summary>
    /// User behavior repository instance.
    /// </summary>
    IUserBehaviorRepository UserBehaviors { get; }
    
    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
