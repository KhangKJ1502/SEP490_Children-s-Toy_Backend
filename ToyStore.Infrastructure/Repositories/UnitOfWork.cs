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
        IBrandRepository brands,
        ITemplateRepository templates)
    {
        _context = context;
        SuperCategories = superCategories;
        Categories = categories;
        Brands = brands;
        Templates = templates;
    }

    public ISuperCategoryRepository SuperCategories { get; }

    public ICategoryRepository Categories { get; }

    public IBrandRepository Brands { get; }

    public ITemplateRepository Templates { get; }

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
