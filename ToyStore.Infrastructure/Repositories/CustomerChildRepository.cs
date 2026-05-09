using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class CustomerChildRepository : ICustomerChildRepository
{
    private readonly SEP490ToyStoreContext _context;

    public CustomerChildRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public Task<List<CustomerChild>> GetActiveByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return _context.CustomerChildren
            .Include(x => x.Sex)
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .OrderBy(x => x.Dob)
            .ThenBy(x => x.ChildId)
            .ToListAsync(cancellationToken);
    }

    public Task<CustomerChild?> GetActiveByIdAsync(int childId, CancellationToken cancellationToken = default)
    {
        return _context.CustomerChildren
            .Include(x => x.Sex)
            .FirstOrDefaultAsync(x => x.ChildId == childId && !x.IsDeleted, cancellationToken);
    }

    public Task AddAsync(CustomerChild child, CancellationToken cancellationToken = default)
    {
        return _context.CustomerChildren.AddAsync(child, cancellationToken).AsTask();
    }

    public Task<int> CountAsync(Expression<Func<CustomerChild, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return _context.CustomerChildren.CountAsync(predicate, cancellationToken);
    }
}
