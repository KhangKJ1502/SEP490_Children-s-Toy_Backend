using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Truy cap du lieu Order cho admin flows.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly SEP490ToyStoreContext _context;

    public OrderRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    // ── Reads ─────────────────────────────────────────────────────────────────

    public async Task<List<Order>> GetAdminPagedAsync(
        IReadOnlyCollection<string> allowedStatusNames,
        int pageNumber,
        int pageSize,
        int? statusId,
        bool assignedToMe,
        int currentAccountId,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = BuildAdminQuery(
            allowedStatusNames, statusId, assignedToMe,
            currentAccountId, keyword, fromDate, toDate);

        return await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAdminAsync(
        IReadOnlyCollection<string> allowedStatusNames,
        int? statusId,
        bool assignedToMe,
        int currentAccountId,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = BuildAdminQuery(
            allowedStatusNames, statusId, assignedToMe,
            currentAccountId, keyword, fromDate, toDate);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdForAdminAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.Status)
            .Include(o => o.Account)
            .Include(o => o.AssignedToStaff)
            .Include(o => o.OrderDetails)
            .Include(o => o.OrderStatusHistories.OrderBy(h => h.CreatedAt))
                .ThenInclude(h => h.ChangedByNavigation)
            .Include(o => o.OrderStatusHistories)
                .ThenInclude(h => h.Status)
            .Include(o => o.ShippingProviderTransactions.OrderByDescending(t => t.CreatedAt))
            .FirstOrDefaultAsync(o => o.OrderId == orderId && !o.IsDeleted, cancellationToken);
    }

    public async Task<Order?> GetByIdForUpdateAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Status)
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && !o.IsDeleted, cancellationToken);
    }

    public async Task<Dictionary<string, byte>> GetStatusMapAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await _context.StatusOrders
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return statuses.ToDictionary(
            s => s.StatusName,
            s => s.StatusId,
            StringComparer.OrdinalIgnoreCase);
    }

    // ── Writes ────────────────────────────────────────────────────────────────

    public async Task AddStatusHistoryAsync(OrderStatusHistory history, CancellationToken cancellationToken = default)
    {
        await _context.OrderStatusHistories.AddAsync(history, cancellationToken);
    }

    public async Task AddShippingTransactionAsync(ShippingProviderTransaction tx, CancellationToken cancellationToken = default)
    {
        await _context.ShippingProviderTransactions.AddAsync(tx, cancellationToken);
    }

    public async Task AddShippingStatusHistoryAsync(ShippingStatusHistory history, CancellationToken cancellationToken = default)
    {
        await _context.ShippingStatusHistories.AddAsync(history, cancellationToken);
    }

    public async Task<ShippingProviderTransaction?> GetShippingTransactionByProviderCodeAsync(
        string providerOrderCode,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShippingProviderTransactions
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.ProviderOrderCode == providerOrderCode, cancellationToken);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private IQueryable<Order> BuildAdminQuery(
        IReadOnlyCollection<string> allowedStatusNames,
        int? statusId,
        bool assignedToMe,
        int currentAccountId,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate)
    {
        IQueryable<Order> query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Status)
            .Include(o => o.AssignedToStaff)
            .Where(o => !o.IsDeleted);

        // Gioi han trang thai theo role neu khong co filter cu the
        if (allowedStatusNames.Count > 0)
        {
            query = query.Where(o => allowedStatusNames.Contains(o.Status.StatusName));
        }

        if (statusId.HasValue)
        {
            query = query.Where(o => o.StatusId == (byte)statusId.Value);
        }

        if (assignedToMe)
        {
            query = query.Where(o => o.AssignedToStaffId == currentAccountId);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(o =>
                o.OrderCode.Contains(kw) ||
                o.ShippingName.Contains(kw) ||
                o.ShippingPhone.Contains(kw));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.OrderDate <= endOfDay);
        }

        return query;
    }
}
