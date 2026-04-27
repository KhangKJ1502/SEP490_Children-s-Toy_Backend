namespace ToyStore.Application.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    ISuperCategoryRepository SuperCategories { get; }

    ICategoryRepository Categories { get; }

<<<<<<< HEAD
    /// <summary>
    /// Repository thao tác với Account.
    /// </summary>
    IAccountRepository Accounts { get; }

    /// <summary>
    /// Lưu tất cả pending changes vào DB.
    /// </summary>
=======
    IBrandRepository Brands { get; }

    ITemplateRepository Templates { get; }

>>>>>>> c3313c693d9206d48e345252f51210905121c246
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
