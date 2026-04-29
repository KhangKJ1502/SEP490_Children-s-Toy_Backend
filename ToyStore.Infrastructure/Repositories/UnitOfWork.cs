using Microsoft.EntityFrameworkCore.Storage;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Unit of Work quản lý transaction và save changes.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SEP490ToyStoreContext _context;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(
        SEP490ToyStoreContext context,
        IVoucherRepository vouchers,
        ISuperCategoryRepository superCategories,
        ICategoryRepository categories,
        IAccountRepository accounts,
        IBrandRepository brands,
        IProductRepository products,
        ITemplateRepository templates)
    {
        _context = context;
        Vouchers = vouchers;
        SuperCategories = superCategories;
        Categories = categories;
        Accounts = accounts;
        Brands = brands;
        Products = products;
        Templates = templates;
    }

    public IVoucherRepository Vouchers { get; }

    public ISuperCategoryRepository SuperCategories { get; }

    public ICategoryRepository Categories { get; }

    public IAccountRepository Accounts { get; }

    public IBrandRepository Brands { get; }

    public IProductRepository Products { get; }

    public ITemplateRepository Templates { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            return;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.CommitAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
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