using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class SavedBankAccountRepository : ISavedBankAccountRepository
{
    private readonly SEP490ToyStoreContext _context;

    public SavedBankAccountRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public Task<List<SavedBankAccount>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return _context.SavedBankAccounts
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.LastUsedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<SavedBankAccount?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.SavedBankAccounts
            .FirstOrDefaultAsync(x => x.SavedBankAccountId == id && !x.IsDeleted, cancellationToken);
    }

    public Task<SavedBankAccount?> GetByUniqueKeyAsync(int accountId, string bankBin, string accountNumber, CancellationToken cancellationToken = default)
    {
        // Don't filter by !IsDeleted, because the service needs to find soft-deleted items to restore (upsert)
        return _context.SavedBankAccounts
            .FirstOrDefaultAsync(x => x.AccountId == accountId && x.BankBin == bankBin && x.AccountNumber == accountNumber, cancellationToken);
    }

    public async Task AddAsync(SavedBankAccount account, CancellationToken cancellationToken = default)
    {
        await _context.SavedBankAccounts.AddAsync(account, cancellationToken);
    }

    public Task<bool> HasPendingWithdrawalRequestsAsync(string bankBin, string accountNumber, CancellationToken cancellationToken = default)
    {
        return _context.WithdrawalRequests
            .AnyAsync(w => w.ToBankBin == bankBin 
                           && w.ToAccountNumber == accountNumber 
                           && (w.Status == "PENDING" || w.Status == "PROCESSING"), 
                       cancellationToken);
    }

    public void Update(SavedBankAccount account)
    {
        _context.SavedBankAccounts.Update(account);
    }
}
