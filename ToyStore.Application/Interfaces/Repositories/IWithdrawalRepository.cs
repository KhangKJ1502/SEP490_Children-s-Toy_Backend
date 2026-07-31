using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IWithdrawalRepository
{
    Task<WithdrawalRequest?> GetByIdAsync(int withdrawalId, CancellationToken ct = default);
    Task<WithdrawalRequest?> GetByReferenceIdAsync(string referenceId, CancellationToken ct = default);
    Task<List<WithdrawalRequest>> GetMyWithdrawalsAsync(int accountId, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountMyWithdrawalsAsync(int accountId, CancellationToken ct = default);

    /// <summary>Returns withdrawals stuck in PENDING or PROCESSING older than the given threshold.</summary>
    Task<List<WithdrawalRequest>> GetStalePendingAsync(DateTime olderThan, CancellationToken ct = default);

    /// <summary>Returns total amount and count of active (PENDING+PROCESSING+SUCCESS) withdrawals today for limit checks.</summary>
    Task<(decimal TotalAmount, int Count)> GetDailyStatsAsync(int accountId, DateTime date, CancellationToken ct = default);

    /// <summary>Returns true if the account already has a PENDING or PROCESSING withdrawal (one-at-a-time rule).</summary>
    Task<bool> HasActivePendingAsync(int accountId, CancellationToken ct = default);

    Task AddAsync(WithdrawalRequest withdrawal, CancellationToken ct = default);

    Task<List<WithdrawalRequest>> GetAdminWithdrawalsAsync(string? keyword, string? status, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAdminWithdrawalsAsync(string? keyword, string? status, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task<WithdrawalRequest?> GetWithDetailsByIdAsync(int id, CancellationToken ct = default);

    // ── Withdrawal Ledger Operations ─────────────────────────────────────────

    /// <summary>
    /// Reads the WithdrawalRequest with a UPDLOCK + ROWLOCK hint to prevent concurrent
    /// ledger operations on the same row. Must be called inside an open transaction.
    /// </summary>
    Task<WithdrawalRequest?> GetForUpdateAsync(int withdrawalId, CancellationToken ct = default);

    /// <summary>
    /// Marks the entity as modified so EF Core will generate an UPDATE statement
    /// on the next SaveChangesAsync call.
    /// </summary>
    void UpdateAsync(WithdrawalRequest withdrawal);

    /// <summary>
    /// Adds a status-transition audit record for the given withdrawal.
    /// </summary>
    Task AddStatusHistoryAsync(WithdrawalStatusHistory history, CancellationToken ct = default);
}
