using System.Threading;
using System.Threading.Tasks;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IWalletTransactionRepository
{
    Task<WalletTransaction> AddAsync(WalletTransaction transaction, CancellationToken cancellationToken = default);
}
