using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.BankAccounts;

namespace ToyStore.Application.Interfaces.Services;

public interface IBankLookupService
{
    Task<Result<List<BankLookupItemDto>>> GetBanksAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> LookupOwnerNameAsync(string bankCode, string accountNumber, CancellationToken cancellationToken = default);
}
