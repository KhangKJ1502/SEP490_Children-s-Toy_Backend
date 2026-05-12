using System.Threading;
using System.Threading.Tasks;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<Wallet> AddAsync(Wallet wallet, CancellationToken cancellationToken = default);
    void Update(Wallet wallet);
}
