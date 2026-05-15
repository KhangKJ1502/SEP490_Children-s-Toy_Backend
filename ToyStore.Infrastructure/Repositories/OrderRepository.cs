using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Truy cap du lieu Order cho admin flows.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly SEP490ToyStoreContext _context;
    private readonly ITimeProvider _timeProvider;

    public OrderRepository(SEP490ToyStoreContext context, ITimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
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

    public async Task<List<Order>> GetCustomerPagedAsync(
        int accountId,
        IReadOnlyCollection<string>? statusNames,
        int pageNumber,
        int pageSize,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = BuildCustomerQuery(accountId, statusNames, keyword, fromDate, toDate);

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

    public async Task<int> CountCustomerAsync(
        int accountId,
        IReadOnlyCollection<string>? statusNames,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = BuildCustomerQuery(accountId, statusNames, keyword, fromDate, toDate);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.Status)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && !o.IsDeleted, cancellationToken);
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

    public async Task<Order?> GetByIdForCustomerAsync(
        int orderId,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.Status)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.Category)
            .Include(o => o.OrderStatusHistories.OrderBy(h => h.CreatedAt))
                .ThenInclude(h => h.Status)
            .Include(o => o.OrderStatusHistories)
                .ThenInclude(h => h.ChangedByNavigation)
            .Include(o => o.ShippingProviderTransactions.OrderByDescending(t => t.CreatedAt))
            .FirstOrDefaultAsync(o =>
                o.OrderId == orderId
                && o.AccountId == accountId
                && !o.IsDeleted,
                cancellationToken);
    }

    public async Task<Order?> GetByIdForUpdateAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Status)
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && !o.IsDeleted, cancellationToken);
    }

    public async Task<Order?> GetByIdWithTrackingAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.ShippingProviderTransactions.OrderByDescending(t => t.CreatedAt))
                .ThenInclude(t => t.ShippingStatusHistories.OrderByDescending(h => h.ProcessedAt))
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

    public async Task<PaymentGatewayTransaction?> GetPaymentTransactionByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        var normalized = requestId.Replace("_", string.Empty);
        return await _context.PaymentGatewayTransactions
            .Include(t => t.Order)
                .ThenInclude(o => o.OrderDetails)
            .Include(t => t.Order)
                .ThenInclude(o => o.Account)
            .FirstOrDefaultAsync(t =>
                t.RequestId == requestId
                || (t.RequestId != null && t.RequestId.Replace("_", string.Empty) == normalized),
                cancellationToken);
    }

    public Task AdjustFlashSaleStockAsync(int slotProductId, int soldDelta, int reservedDelta, CancellationToken cancellationToken = default)
    {
        return _context.Database.ExecuteSqlRawAsync(
            @"UPDATE PromotionProductSlots 
              SET SoldQuantity = CASE WHEN SoldQuantity + {0} >= 0 THEN SoldQuantity + {0} ELSE 0 END,
                  ReservedQuantity = CASE WHEN ReservedQuantity + {1} >= 0 THEN ReservedQuantity + {1} ELSE 0 END
              WHERE SlotProductID = {2}",
            new object[] { soldDelta, reservedDelta, slotProductId },
            cancellationToken);
    }

    public async Task RefundWalletAsync(int accountId, decimal amount, string transactionId, CancellationToken cancellationToken = default)
    {
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.AccountId == accountId, cancellationToken);
        if (wallet == null) return;

        var idempotencyKey = $"REFUND_{transactionId}";
        var alreadyRefunded = await _context.WalletTransactions.AnyAsync(wt => wt.IdempotencyKey == idempotencyKey, cancellationToken);
        if (alreadyRefunded) return;

        var balanceBefore = wallet.Balance;
        wallet.Balance += amount;
        var now = _timeProvider.UtcNow;

        await _context.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = wallet.WalletId,
            AccountId = accountId,
            TxnType = "Refund",
            Direction = "CR",
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = wallet.Balance,
            Method = "Wallet",
            IdempotencyKey = idempotencyKey,
            Status = "Completed",
            CreatedAt = now,
            CompletedAt = now
        }, cancellationToken);
    }

    public async Task RestoreVoucherAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var usageLogs = await _context.VoucherUsageLogs.Where(l => l.OrderId == orderId).ToListAsync(cancellationToken);
        foreach (var usageLog in usageLogs)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE Vouchers SET UsedQuantity = CASE WHEN UsedQuantity > 0 THEN UsedQuantity - 1 ELSE 0 END WHERE VoucherID = {0}",
                new object[] { usageLog.VoucherId },
                cancellationToken);
            _context.VoucherUsageLogs.Remove(usageLog);
        }
    }

    public async Task AddPaymentHistoryAsync(PaymentHistory history, CancellationToken cancellationToken = default)
    {
        await _context.PaymentHistories.AddAsync(history, cancellationToken);
    }

    public async Task<Wallet?> GetWalletByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets.FirstOrDefaultAsync(w => w.AccountId == accountId, cancellationToken);
    }

    public async Task AddWalletTransactionAsync(WalletTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.WalletTransactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<bool> ExistsWalletTransactionByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.WalletTransactions.AnyAsync(wt => wt.IdempotencyKey == idempotencyKey, cancellationToken);
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

    private IQueryable<Order> BuildCustomerQuery(
        int accountId,
        IReadOnlyCollection<string>? statusNames,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate)
    {
        IQueryable<Order> query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Status)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.Category)
            .Where(o => o.AccountId == accountId && !o.IsDeleted);

        if (statusNames is { Count: > 0 })
        {
            query = query.Where(o => statusNames.Contains(o.Status.StatusName));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(o =>
                o.OrderCode.Contains(kw)
                || o.OrderDetails.Any(d => d.ProductName.Contains(kw)));
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
