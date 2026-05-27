using ToyStore.Application.Common.Helpers;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository thao tac du lieu Order va cac bang lien quan.
/// </summary>
public interface IOrderRepository
{
    // ── Reads ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lay danh sach don hang co phan trang, loc theo tien ich admin.
    /// </summary>
    Task<List<Order>> GetAdminPagedAsync(
        IReadOnlyCollection<string> allowedStatusNames,
        int pageNumber,
        int pageSize,
        int? statusId,
        IReadOnlyCollection<int>? statusIds,
        bool restrictToAssignment,
        int currentAccountId,
        byte assignmentRoleId,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh sach don hang cua customer co phan trang.
    /// </summary>
    Task<List<Order>> GetCustomerPagedAsync(
        int accountId,
        IReadOnlyCollection<string>? statusNames,
        int pageNumber,
        int pageSize,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dem tong so don hang theo dieu kien admin.
    /// </summary>
    Task<int> CountAdminAsync(
        IReadOnlyCollection<string> allowedStatusNames,
        int? statusId,
        IReadOnlyCollection<int>? statusIds,
        bool restrictToAssignment,
        int currentAccountId,
        byte assignmentRoleId,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dem tong so don hang cua customer theo dieu kien.
    /// </summary>
    Task<int> CountCustomerAsync(
        int accountId,
        IReadOnlyCollection<string>? statusNames,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay don hang theo ID (lightweight, AsNoTracking). Dung cho notification handlers.
    /// </summary>
    Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay chi tiet don hang day du (OrderDetails, Status, AssignedToStaff, Account).
    /// Tra ve null neu khong tim thay hoac da bi xoa mem.
    /// </summary>
    Task<Order?> GetByIdForAdminAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin detail query scoped to an active OrderAssignments row for Staff/Merchandise.
    /// </summary>
    Task<Order?> GetByIdForAssignedOperationalAsync(
        int orderId,
        int accountId,
        byte assignmentRoleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay chi tiet don hang day du cho customer (co kiem tra owner).
    /// </summary>
    Task<Order?> GetByIdForCustomerAsync(int orderId, int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay don hang de xu ly trang thai (tracking lock, khong AsNoTracking).
    /// Tra ve null neu khong tim thay hoac da bi xoa mem.
    /// </summary>
    Task<Order?> GetByIdForUpdateAsync(int orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdWithTrackingAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay map StatusName -> StatusID tu bang StatusOrders.
    /// </summary>
    Task<Dictionary<string, byte>> GetStatusMapAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh sach cac item kem theo khoi luong va kich thuoc cua don hang de tinh phi van chuyen.
    /// </summary>
    Task<List<ShippingItem>> GetShippingItemsForOrderAsync(int orderId, CancellationToken cancellationToken = default);

    // ── Writes ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Them ban ghi OrderStatusHistory.
    /// </summary>
    Task AddStatusHistoryAsync(OrderStatusHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Them ban ghi ShippingProviderTransaction.
    /// </summary>
    Task AddShippingTransactionAsync(ShippingProviderTransaction tx, CancellationToken cancellationToken = default);

    /// <summary>
    /// Them ban ghi ShippingStatusHistory.
    /// </summary>
    Task AddShippingStatusHistoryAsync(ShippingStatusHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tim ShippingProviderTransaction theo ProviderOrderCode.
    /// </summary>
    Task<ShippingProviderTransaction?> GetShippingTransactionByProviderCodeAsync(
        string providerOrderCode,
        CancellationToken cancellationToken = default);

    Task<PaymentGatewayTransaction?> GetPaymentTransactionByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat ton kho Flash Sale atomic.
    /// </summary>
    Task AdjustFlashSaleStockAsync(int slotProductId, int soldDelta, int reservedDelta, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hoan tien vao vi (atomic).
    /// </summary>
    Task RefundWalletAsync(int accountId, decimal amount, string transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hoan lai voucher (atomic).
    /// </summary>
    Task RestoreVoucherAsync(int orderId, CancellationToken cancellationToken = default);

    Task AddPaymentHistoryAsync(PaymentHistory history, CancellationToken cancellationToken = default);

    Task<bool> ExistsShippingStatusHistoryAsync(
        long shippingTxId, string newStatus, string rawPayload, CancellationToken cancellationToken = default);

    Task<int> CountShippingStatusHistoryAsync(
        long shippingTxId, string newStatus, CancellationToken cancellationToken = default);

    Task<Wallet?> GetWalletByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task AddWalletTransactionAsync(WalletTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> ExistsWalletTransactionByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}
