using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Common.Helpers;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Services;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
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
        IReadOnlyCollection<int>? statusIds,
        bool restrictToAssignment,
        int currentAccountId,
        byte assignmentRoleId,
        string? assignmentScope,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = BuildAdminQuery(
            allowedStatusNames, statusId, statusIds, restrictToAssignment,
            currentAccountId, assignmentRoleId, assignmentScope, keyword, fromDate, toDate);

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
        IReadOnlyCollection<int>? statusIds,
        bool restrictToAssignment,
        int currentAccountId,
        byte assignmentRoleId,
        string? assignmentScope,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = BuildAdminQuery(
            allowedStatusNames, statusId, statusIds, restrictToAssignment,
            currentAccountId, assignmentRoleId, assignmentScope, keyword, fromDate, toDate);

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
        return await BuildAdminDetailQuery()
            .FirstOrDefaultAsync(o => o.OrderId == orderId && !o.IsDeleted, cancellationToken);
    }

    public async Task<Order?> GetByIdForAssignedOperationalAsync(
        int orderId,
        int accountId,
        byte assignmentRoleId,
        CancellationToken cancellationToken = default)
    {
        var processedStatuses = OrderStatuses.GetProcessedMilestoneStatuses(assignmentRoleId);

        return await BuildAdminDetailQuery()
            .Where(o => o.OrderId == orderId
                        && !o.IsDeleted
                        && (
                            _context.OrderAssignments.Any(oa =>
                                oa.OrderId == o.OrderId
                                && oa.AccountId == accountId
                                && oa.RoleId == assignmentRoleId
                                && oa.IsActive)
                            || (
                                _context.OrderAssignments.Any(oa =>
                                    oa.OrderId == o.OrderId
                                    && oa.AccountId == accountId
                                    && oa.RoleId == assignmentRoleId)
                                && _context.OrderStatusHistories.Any(h =>
                                    h.OrderId == o.OrderId
                                    && h.ChangedBy == accountId
                                    && h.Status != null
                                    && processedStatuses.Contains(h.Status.StatusName)))))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<Order> BuildAdminDetailQuery()
    {
        return _context.Orders
            .AsNoTracking()
            .Include(o => o.Status)
            .Include(o => o.Account)
            .Include(o => o.AssignedToStaff)
            .Include(o => o.AssignedToMerch)
            .Include(o => o.CancelledByNavigation)
            .Include(o => o.OrderDetails)
            .Include(o => o.OrderStatusHistories.OrderBy(h => h.HistoryId))
                .ThenInclude(h => h.ChangedByNavigation)
            .Include(o => o.OrderStatusHistories)
                .ThenInclude(h => h.Status)
            .Include(o => o.ShippingProviderTransactions.OrderByDescending(t => t.CreatedAt))
                .ThenInclude(t => t.ShippingStatusHistories.OrderByDescending(h => h.ProcessedAt));
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
            .Include(o => o.OrderStatusHistories.OrderBy(h => h.HistoryId))
                .ThenInclude(h => h.Status)
            .Include(o => o.OrderStatusHistories)
                .ThenInclude(h => h.ChangedByNavigation)
            .Include(o => o.ShippingProviderTransactions.OrderByDescending(t => t.CreatedAt))
            .Include(o => o.OrderRefunds)
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
            .Include(o => o.Status)
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

    public async Task<List<ShippingItem>> GetShippingItemsForOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderDetails
            .Where(od => od.OrderId == orderId)
            .Join(_context.Products,
                  od => od.ProductId, p => p.ProductId,
                  (od, p) => new { od, p })
            .Join(_context.ProductDetails,
                  x => x.p.ProductId, pd => pd.ProductId,
                  (x, pd) => new { x.od, x.p, pd })
            .Join(_context.Categories,
                  x => x.p.CategoryId, c => c.CategoryId,
                  (x, c) => new ShippingItem(
                      x.od.ProductId,
                      x.od.ProductName,
                      c.CategoryName,
                      x.od.Quantity,
                      x.od.UnitPrice,
                      x.pd.WeightGram,
                      x.pd.LengthCm,
                      x.pd.WidthCm,
                      x.pd.HeightCm
                  ))
            .ToListAsync(cancellationToken);
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

    public async Task<bool> ExistsShippingStatusHistoryAsync(
        long shippingTxId, string newStatus, string rawPayload, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingStatusHistories.AnyAsync(
            h => h.ShippingTxId == shippingTxId
                 && h.NewStatus == newStatus
                 && h.RawPayload == rawPayload,
            cancellationToken);
    }

    public async Task<int> CountShippingStatusHistoryAsync(
        long shippingTxId, string newStatus, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingStatusHistories.CountAsync(
            h => h.ShippingTxId == shippingTxId && h.NewStatus == newStatus,
            cancellationToken);
    }

    public async Task<ShippingProviderTransaction?> GetShippingTransactionByProviderCodeAsync(
        string providerOrderCode,
        CancellationToken cancellationToken = default)
    {
        var code = providerOrderCode.Trim();
        return await _context.ShippingProviderTransactions
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t =>
                t.ProviderOrderCode == code
                || t.TrackingNumber == code
                || (t.Order != null && t.Order.ShippingOrderCode == code),
                cancellationToken);
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
            Method = "Internal",
            Reason = $"Refund for order {transactionId}",
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

    public async Task<long?> GetWalletTransactionIdByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.WalletTransactions
            .Where(wt => wt.IdempotencyKey == idempotencyKey)
            .Select(wt => (long?)wt.WalletTransactionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasCompletedRefundWalletCreditForOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.WalletTransactions.AnyAsync(
            wt => wt.RelatedOrderId == orderId
                  && wt.TxnType == WalletTxnTypes.Refund
                  && wt.Direction == WalletTxnDirections.Credit
                  && wt.Status == "Completed",
            cancellationToken);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static readonly string[] AdminGhnReturnStatuses =
    [
        ShippingStatuses.WaitingToReturn,
        ShippingStatuses.Return,
        ShippingStatuses.ReturnTransporting,
        ShippingStatuses.ReturnSorting,
        ShippingStatuses.Returning,
        ShippingStatuses.ReturnFail,
        ShippingStatuses.Returned,
    ];

    private IQueryable<Order> BuildAdminQuery(
        IReadOnlyCollection<string> allowedStatusNames,
        int? statusId,
        IReadOnlyCollection<int>? statusIds,
        bool restrictToAssignment,
        int currentAccountId,
        byte assignmentRoleId,
        string? assignmentScope,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate)
    {
        IQueryable<Order> query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Status)
            .Include(o => o.AssignedToStaff)
            .Include(o => o.AssignedToMerch)
            .Include(o => o.ShippingProviderTransactions)
            .Where(o => !o.IsDeleted);

        if (allowedStatusNames.Count > 0)
        {
            query = query.Where(o => allowedStatusNames.Contains(o.Status.StatusName));
        }

        if (statusIds is { Count: > 0 })
        {
            query = ApplyAdminStatusFilter(query, statusIds);
        }
        else if (statusId.HasValue)
        {
            query = ApplyAdminStatusFilter(query, [statusId.Value]);
        }

        if (restrictToAssignment)
        {
            query = ApplyStaffMerchAssignmentScope(
                query,
                currentAccountId,
                assignmentRoleId,
                assignmentScope);
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

        var hasExplicitStatusFilter = statusId.HasValue || statusIds is { Count: > 0 };
        if (!hasExplicitStatusFilter && string.IsNullOrWhiteSpace(keyword))
        {
            // Chỉ ẩn các đơn SE_PAY chưa thanh toán (rác). Các đơn COD/Wallet dù bị Hủy vẫn hiện để theo dõi.
            query = query.Where(o => !(o.PaymentMethod == "SE_PAY" && o.PaymentStatus != "PAID"));
        }

        return query;
    }

    private IQueryable<Order> ApplyStaffMerchAssignmentScope(
        IQueryable<Order> query,
        int currentAccountId,
        byte assignmentRoleId,
        string? assignmentScope)
    {
        var scope = OrderAssignmentScopes.Normalize(assignmentScope);
        var processedStatuses = OrderStatuses.GetProcessedMilestoneStatuses(assignmentRoleId);
        var completedTabStatuses = OrderStatuses.StaffMerchCompletedTabStatuses;

        if (scope == OrderAssignmentScopes.Completed)
        {
            return query.Where(o =>
                _context.OrderAssignments.Any(oa =>
                    oa.OrderId == o.OrderId
                    && oa.AccountId == currentAccountId
                    && oa.RoleId == assignmentRoleId)
                && _context.OrderStatusHistories.Any(h =>
                    h.OrderId == o.OrderId
                    && h.ChangedBy == currentAccountId
                    && h.Status != null
                    && processedStatuses.Contains(h.Status.StatusName))
                && (
                    !_context.OrderAssignments.Any(oa =>
                        oa.OrderId == o.OrderId
                        && oa.AccountId == currentAccountId
                        && oa.RoleId == assignmentRoleId
                        && oa.IsActive)
                    || completedTabStatuses.Contains(o.Status.StatusName)));
        }

        return query.Where(o =>
            o.Status.StatusName != OrderStatuses.Completed
            && _context.OrderAssignments.Any(oa =>
                oa.OrderId == o.OrderId
                && oa.AccountId == currentAccountId
                && oa.RoleId == assignmentRoleId
                && oa.IsActive));
    }

    private IQueryable<Order> ApplyAdminStatusFilter(IQueryable<Order> query, IReadOnlyCollection<int> filterStatusIds)
    {
        var ids = filterStatusIds.Select(i => (byte)i).Distinct().ToList();
        var expandedIds = ExpandAdminDeliveringGroup(ids);
        var includesDeliveringGroup = ids.Contains(AdminOrderFulfillmentMapper.DeliveringStatusId);

        if (includesDeliveringGroup)
        {
            return query.Where(o =>
                (expandedIds.Contains(o.StatusId)
                || o.ShippingProviderTransactions.Any(t =>
                    t.Status != null && AdminGhnReturnStatuses.Contains(t.Status)))
                && o.Status.StatusName != OrderStatuses.Cancelled
                && o.Status.StatusName != OrderStatuses.Refunded
                && o.Status.StatusName != OrderStatuses.Completed
                && o.Status.StatusName != OrderStatuses.Delivered);
        }

        return query.Where(o => ids.Contains(o.StatusId));
    }

    private static List<byte> ExpandAdminDeliveringGroup(List<byte> ids)
    {
        if (!ids.Contains(AdminOrderFulfillmentMapper.DeliveringStatusId))
            return ids;

        var expanded = new HashSet<byte>(ids);
        foreach (var id in AdminOrderFulfillmentMapper.DeliveringGroupStatusIds)
            expanded.Add(id);
        return expanded.ToList();
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
            .Include(o => o.ShippingProviderTransactions)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.Category)
            .Include(o => o.OrderRefunds)
            .Where(o => o.AccountId == accountId && !o.IsDeleted);

        if (statusNames is { Count: > 0 })
        {
            var isDeliveringTab = statusNames.Contains(OrderStatuses.Delivering)
                || statusNames.Contains(OrderStatuses.Returning)
                || statusNames.Contains(OrderStatuses.ReturnCompleted);

            if (isDeliveringTab)
            {
                var ghnReturnStatuses = new[]
                {
                    ShippingStatuses.WaitingToReturn,
                    ShippingStatuses.Return,
                    ShippingStatuses.ReturnTransporting,
                    ShippingStatuses.ReturnSorting,
                    ShippingStatuses.Returning,
                    ShippingStatuses.ReturnFail,
                    ShippingStatuses.Returned,
                };

                query = query.Where(o =>
                    (statusNames.Contains(o.Status.StatusName)
                    || o.ShippingProviderTransactions.Any(t =>
                        t.Status != null && ghnReturnStatuses.Contains(t.Status.ToLower())))
                    && o.Status.StatusName != OrderStatuses.Cancelled
                    && o.Status.StatusName != OrderStatuses.Refunded
                    && o.Status.StatusName != OrderStatuses.Completed
                    && o.Status.StatusName != OrderStatuses.Delivered);
            }
            else
            {
                query = query.Where(o => statusNames.Contains(o.Status.StatusName));
            }

            // Nếu đang xem tab Bị hủy, chỉ hiện các đơn thực sự (COD, WALLET, paid/refunded SE_PAY) và ẩn rác SE_PAY chưa thanh toán
            if (statusNames.Contains(OrderStatuses.Cancelled))
            {
                query = query.Where(o => o.Status.StatusName != OrderStatuses.Cancelled
                                      || o.PaymentMethod == "SHIP_COD"
                                      || o.PaymentMethod == "WALLET"
                                      || (o.PaymentMethod == "SE_PAY" && (o.PaymentStatus == "PAID" || o.PaymentStatus == "REFUNDED")));
            }
        }
        else
        {
            // Tab "All": ẩn rác (SE_PAY đã cancel chưa thanh toán) nhưng vẫn hiện SE_PAY PENDING
            // để user thấy đơn đang chờ thanh toán và có thể continue hoặc cancel.
            query = query.Where(o => !(o.Status.StatusName == OrderStatuses.Cancelled
                                      && o.PaymentMethod == "SE_PAY"
                                      && o.PaymentStatus != "PAID"
                                      && o.PaymentStatus != "REFUNDED")
                                  && (o.PaymentMethod != "SE_PAY"
                                      || o.PaymentStatus == "PAID"
                                      || o.PaymentStatus == "REFUNDED"
                                      || o.PaymentStatus == "PENDING"));
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
