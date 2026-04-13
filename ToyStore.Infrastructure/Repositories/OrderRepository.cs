using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Order repository implementation.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly ToyStoreDbContext _context;
    private readonly DbSet<Order> _dbSet;
    
    public OrderRepository(ToyStoreDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Order>();
    }
    
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);
    }
    
    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => !o.IsDeleted)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(order, cancellationToken);
        return order;
    }
    
    public void Update(Order order)
    {
        _dbSet.Update(order);
    }
    
    public void Remove(Order order)
    {
        _dbSet.Remove(order);
    }
    
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.User)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && !o.IsDeleted, cancellationToken);
    }
    
    public async Task<Order?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.User)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);
    }
    
    public async Task<IReadOnlyList<Order>> GetByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.Items)
            .Where(o => o.UserId == userId && !o.IsDeleted)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Order>> GetByStatusAsync(
        OrderStatus status, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.User)
            .Where(o => o.Status == status && !o.IsDeleted)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Order>> GetByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.User)
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && !o.IsDeleted)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Order>> GetRecentOrdersAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.User)
            .Where(o => !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Order>> GetPendingShipmentAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new[] { OrderStatus.Confirmed, OrderStatus.Processing };
        return await _dbSet
            .Include(o => o.User)
            .Include(o => o.Items)
            .Where(o => statuses.Contains(o.Status) && !o.IsDeleted)
            .OrderBy(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? userId = null,
        OrderStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(o => o.User).Where(o => !o.IsDeleted);
        
        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);
            
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);
            
        if (startDate.HasValue)
            query = query.Where(o => o.OrderDate >= startDate.Value);
            
        if (endDate.HasValue)
            query = query.Where(o => o.OrderDate <= endDate.Value);
            
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(o => 
                o.OrderNumber.Contains(searchTerm) ||
                o.RecipientName.Contains(searchTerm) ||
                o.RecipientPhone.Contains(searchTerm));
        }
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return (items, totalCount);
    }
    
    public async Task<IReadOnlyList<Order>> GetPendingOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Pending && !o.IsDeleted)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
