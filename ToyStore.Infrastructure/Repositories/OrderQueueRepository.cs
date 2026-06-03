using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class OrderQueueRepository : IOrderQueueRepository
{
    private readonly SEP490ToyStoreContext _context;

    public OrderQueueRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public Task<List<OrderQueue>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return _context.OrderQueues
            .AsNoTracking()
            .Include(x => x.Order)
                .ThenInclude(o => o.Status)
            .Where(x => !x.IsResolved)
            .OrderBy(x => x.QueuedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<OrderQueue?> GetByIdAsync(int queueId, CancellationToken cancellationToken = default)
    {
        return _context.OrderQueues
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.QueueId == queueId, cancellationToken);
    }

    public Task<OrderQueue?> GetOldestPendingAsync(CancellationToken cancellationToken = default)
    {
        return _context.OrderQueues
            .Include(x => x.Order)
            .Where(x => !x.IsResolved)
            .OrderBy(x => x.QueuedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExistsPendingForOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return _context.OrderQueues
            .AsNoTracking()
            .AnyAsync(x => x.OrderId == orderId && !x.IsResolved, cancellationToken);
    }

    public async Task<OrderQueue> CreateAsync(OrderQueue entry, CancellationToken cancellationToken = default)
    {
        await _context.OrderQueues.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task MarkResolvedAsync(int queueId, int assignedBy, CancellationToken cancellationToken = default)
    {
        var entry = await _context.OrderQueues
            .FirstOrDefaultAsync(x => x.QueueId == queueId, cancellationToken);
        if (entry is null) return;

        entry.IsResolved = true;
        entry.AssignedBy = assignedBy;
        entry.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
