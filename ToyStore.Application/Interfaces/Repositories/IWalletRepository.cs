
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
}
