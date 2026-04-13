namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Unit of Work pattern — quản lý transaction, được inject vào Service layer.
/// Repositories sẽ được thêm vào đây khi từng feature được implement.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Lưu tất cả pending changes vào DB.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Bắt đầu transaction.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit transaction.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback transaction.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
