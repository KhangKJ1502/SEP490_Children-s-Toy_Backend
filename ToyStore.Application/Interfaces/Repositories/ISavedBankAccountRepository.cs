using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface ISavedBankAccountRepository
{
    Task<List<SavedBankAccount>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<SavedBankAccount?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SavedBankAccount?> GetByUniqueKeyAsync(int accountId, string bankBin, string accountNumber, CancellationToken cancellationToken = default);
    Task AddAsync(SavedBankAccount account, CancellationToken cancellationToken = default);
    Task<bool> HasPendingWithdrawalRequestsAsync(string bankBin, string accountNumber, CancellationToken cancellationToken = default);
    void Update(SavedBankAccount account);
}

