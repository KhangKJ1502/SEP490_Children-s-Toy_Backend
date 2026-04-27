using Microsoft.EntityFrameworkCore.Storage;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly SEP490ToyStoreContext _context;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(
        SEP490ToyStoreContext context,
        ISuperCategoryRepository superCategories,
        ICategoryRepository categories,
<<<<<<< HEAD
        IAccountRepository accounts)
=======
        IBrandRepository brands,
        ITemplateRepository templates)
>>>>>>> c3313c693d9206d48e345252f51210905121c246
    {
        _context = context;
        SuperCategories = superCategories;
        Categories = categories;
<<<<<<< HEAD
        Accounts = accounts;
=======
        Brands = brands;
        Templates = templates;
>>>>>>> c3313c693d9206d48e345252f51210905121c246
    }

    public ISuperCategoryRepository SuperCategories { get; }

    public ICategoryRepository Categories { get; }

<<<<<<< HEAD
    public IAccountRepository Accounts { get; }
=======
    public IBrandRepository Brands { get; }

    public ITemplateRepository Templates { get; }
>>>>>>> c3313c693d9206d48e345252f51210905121c246

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            return;
        }

        await _currentTransaction.CommitAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            return;
        }

        await _currentTransaction.RollbackAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
    }
}
