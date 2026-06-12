using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.BankAccounts;

namespace ToyStore.Application.Interfaces.Services;

public interface ISavedBankAccountService
{
    Task<Result<List<SavedBankAccountDto>>> GetMySavedBankAccountsAsync(CancellationToken cancellationToken = default);
    Task<Result<SavedBankAccountDto>> CreateSavedBankAccountAsync(CreateSavedBankAccountDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteSavedBankAccountAsync(int id, CancellationToken cancellationToken = default);
    Task<Result> SetDefaultBankAccountAsync(int id, CancellationToken cancellationToken = default);
}
