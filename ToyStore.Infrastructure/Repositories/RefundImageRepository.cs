using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class RefundImageRepository : IRefundImageRepository
{
    private readonly SEP490ToyStoreContext _context;
    private readonly DbSet<RefundImage> _dbSet;

    public RefundImageRepository(SEP490ToyStoreContext context)
    {
        _context = context;
        _dbSet = context.Set<RefundImage>();
    }

    public async Task AddRangeAsync(IEnumerable<RefundImage> images, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(images, cancellationToken);
    }
}
