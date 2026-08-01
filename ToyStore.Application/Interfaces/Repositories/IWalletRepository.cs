
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);

    Task<Wallet?> GetByAccountIdWithActivePinAsync(int accountId, CancellationToken cancellationToken = default);

    Task<WalletPin?> GetActivePinByWalletIdAsync(int walletId, CancellationToken cancellationToken = default);

    Task<List<Wallet>> GetAdminPagedAsync(
        int pageNumber,
        int pageSize,
        string? accountSearchTerm,
        string? status,
        CancellationToken cancellationToken = default);

    Task<int> CountAdminAsync(
        string? accountSearchTerm,
        string? status,
        CancellationToken cancellationToken = default);

    Task<Wallet?> GetByIdWithAccountAsync(int walletId, CancellationToken cancellationToken = default);

    Task<Wallet?> GetAdminByIdAsync(int walletId, CancellationToken cancellationToken = default);

    Task<Wallet> CreateAsync(Wallet wallet, CancellationToken cancellationToken = default);

    Task AddPinAsync(WalletPin walletPin, CancellationToken cancellationToken = default);

    Task AddPinAttemptAsync(WalletPinAttempt attempt, CancellationToken cancellationToken = default);

    Task<int> CountTransactionsByWalletIdAsync(int walletId, CancellationToken cancellationToken = default);

    Task<List<WalletTransaction>> GetTransactionsByWalletIdAsync(
        int walletId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    void UpdateWallet(Wallet wallet);

    void UpdatePin(WalletPin walletPin);

    Task DeactivateActivePinsAsync(int walletId, CancellationToken cancellationToken = default);

    // ── Withdrawal Ledger Operations ─────────────────────────────────────────

    /// <summary>
    /// Reads the wallet with a UPDLOCK + ROWLOCK hint to prevent concurrent race conditions
    /// during the withdrawal ledger operations. Must be called inside an open transaction.
    /// </summary>
    Task<Wallet?> GetForUpdateAsync(int accountId, CancellationToken ct = default);

    /// <summary>
    /// Atomically increments LockedBalance by <paramref name="amount"/>.
    /// Uses a conditional SQL UPDATE to prevent over-locking beyond available balance.
    /// Returns the number of rows affected (0 = insufficient available balance).
    /// </summary>
    Task<int> IncrementLockedBalanceAsync(int accountId, decimal amount, CancellationToken ct = default);

    /// <summary>
    /// Atomically decrements LockedBalance by <paramref name="amount"/>.
    /// Used during Rollback and Cancel to release previously locked funds.
    /// Returns the number of rows affected (0 = LockedBalance would go negative).
    /// </summary>
    Task<int> DecrementLockedBalanceAsync(int accountId, decimal amount, CancellationToken ct = default);

    /// <summary>
    /// Atomically deducts <paramref name="amount"/> from both Balance and LockedBalance.
    /// Used during Commit after a successful PayOS payout.
    /// Returns the number of rows affected (0 = insufficient Balance or LockedBalance).
    /// </summary>
    Task<int> CommitDeductionAsync(int walletId, decimal amount, CancellationToken ct = default);

    /// <summary>
    /// Adds a WalletTransaction record (journal entry) for the withdrawal deduction.
    /// </summary>
    Task AddTransactionAsync(WalletTransaction transaction, CancellationToken ct = default);
}
