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
        bool assignedToMe,
        int currentAccountId,
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
        bool assignedToMe,
        int currentAccountId,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay chi tiet don hang day du (OrderDetails, Status, AssignedToStaff, Account).
    /// Tra ve null neu khong tim thay hoac da bi xoa mem.
    /// </summary>
    Task<Order?> GetByIdForAdminAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay don hang de xu ly trang thai (tracking lock, khong AsNoTracking).
    /// Tra ve null neu khong tim thay hoac da bi xoa mem.
    /// </summary>
    Task<Order?> GetByIdForUpdateAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay map StatusName -> StatusID tu bang StatusOrders.
    /// </summary>
    Task<Dictionary<string, byte>> GetStatusMapAsync(CancellationToken cancellationToken = default);

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
}
