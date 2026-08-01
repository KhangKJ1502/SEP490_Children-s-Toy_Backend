using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly SEP490ToyStoreContext _context;

    public WalletRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public Task<Wallet?> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return _context.Wallets.FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
    }

    public Task<Wallet?> GetByAccountIdWithActivePinAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return _context.Wallets
            .Include(x => x.WalletPins.Where(p => p.IsActive))
            .FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
    }

    public Task<WalletPin?> GetActivePinByWalletIdAsync(int walletId, CancellationToken cancellationToken = default)
    {
        return _context.WalletPins
            .FirstOrDefaultAsync(x => x.WalletId == walletId && x.IsActive, cancellationToken);
    }

    public Task<List<Wallet>> GetAdminPagedAsync(
        int pageNumber,
        int pageSize,
        string? accountSearchTerm,
        string? status,
        CancellationToken cancellationToken = default)
    {
        return BuildAdminQuery(accountSearchTerm, status)
            .OrderBy(x => x.Status == "Frozen" ? 0 : 1)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAdminAsync(
        string? accountSearchTerm,
        string? status,
        CancellationToken cancellationToken = default)
    {
        return BuildAdminQuery(accountSearchTerm, status).CountAsync(cancellationToken);
    }

    public Task<Wallet?> GetByIdWithAccountAsync(int walletId, CancellationToken cancellationToken = default)
    {
        return _context.Wallets
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.WalletId == walletId, cancellationToken);
    }

    public Task<Wallet?> GetAdminByIdAsync(int walletId, CancellationToken cancellationToken = default)
    {
        return BuildAdminQuery(null, null)
            .FirstOrDefaultAsync(x => x.WalletId == walletId, cancellationToken);
    }

    public async Task<Wallet> CreateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        await _context.Wallets.AddAsync(wallet, cancellationToken);
        return wallet;
    }

    public async Task AddPinAsync(WalletPin walletPin, CancellationToken cancellationToken = default)
    {
        await _context.WalletPins.AddAsync(walletPin, cancellationToken);
    }

    public async Task AddPinAttemptAsync(WalletPinAttempt attempt, CancellationToken cancellationToken = default)
    {
        await _context.WalletPinAttempts.AddAsync(attempt, cancellationToken);
    }

    public Task<int> CountTransactionsByWalletIdAsync(int walletId, CancellationToken cancellationToken = default)
    {
        return _context.WalletTransactions
            .Where(x => x.WalletId == walletId)
            .CountAsync(cancellationToken);
    }

    public Task<List<WalletTransaction>> GetTransactionsByWalletIdAsync(
        int walletId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return _context.WalletTransactions
            .Include(x => x.RelatedOrder)
            .Where(x => x.WalletId == walletId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public void UpdateWallet(Wallet wallet)
    {
        _context.Wallets.Update(wallet);
    }

    public void UpdatePin(WalletPin walletPin)
    {
        _context.WalletPins.Update(walletPin);
    }

    public async Task DeactivateActivePinsAsync(int walletId, CancellationToken cancellationToken = default)
    {
        var activePins = await _context.WalletPins
            .Where(x => x.WalletId == walletId && x.IsActive)
            .ToListAsync(cancellationToken);

        if (activePins.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var pin in activePins)
        {
            pin.IsActive = false;
            pin.UpdatedAt = now;
            _context.WalletPins.Update(pin);
        }
    }

    // ── Withdrawal Ledger Operations ─────────────────────────────────────────

    public async Task<Wallet?> GetForUpdateAsync(int accountId, CancellationToken ct = default)
    {
        // Issue UPDLOCK hint to prevent concurrent updates during the same transaction
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM Wallets WITH (UPDLOCK, ROWLOCK) WHERE AccountID = {accountId}", ct);
        return await _context.Wallets.FirstOrDefaultAsync(w => w.AccountId == accountId, ct);
    }

    public Task<int> IncrementLockedBalanceAsync(int accountId, decimal amount, CancellationToken ct = default)
        => _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Wallets SET LockedBalance = LockedBalance + {amount} WHERE AccountID = {accountId} AND (Balance - LockedBalance) >= {amount}",
            ct);

    public Task<int> DecrementLockedBalanceAsync(int accountId, decimal amount, CancellationToken ct = default)
        => _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Wallets SET LockedBalance = LockedBalance - {amount} WHERE AccountID = {accountId} AND LockedBalance >= {amount}",
            ct);

    public Task<int> CommitDeductionAsync(int walletId, decimal amount, CancellationToken ct = default)
        => _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Wallets SET Balance = Balance - {amount}, LockedBalance = LockedBalance - {amount} WHERE WalletID = {walletId} AND Balance >= {amount} AND LockedBalance >= {amount}",
            ct);

    public async Task AddTransactionAsync(WalletTransaction transaction, CancellationToken ct = default)
        => await _context.WalletTransactions.AddAsync(transaction, ct);

    private IQueryable<Wallet> BuildAdminQuery(string? accountSearchTerm, string? status)
    {
        var query = _context.Wallets
            .Include(x => x.Account)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(accountSearchTerm))
        {
            var term = accountSearchTerm.Trim();
            query = query.Where(x =>
                x.Account.AccountName.Contains(term)
                || x.Account.Email.Contains(term)
                || (x.Account.PhoneNumber != null && x.Account.PhoneNumber.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        return query;
    }
}
