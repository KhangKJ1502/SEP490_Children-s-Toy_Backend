using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class WalletTransactionRepository : IWalletTransactionRepository
{
    private readonly SEP490ToyStoreContext _context;

    public WalletTransactionRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<WalletTransaction> AddAsync(WalletTransaction transaction, CancellationToken cancellationToken = default)
    {
        var result = await _context.WalletTransactions.AddAsync(transaction, cancellationToken);
        return result.Entity;
    }
}
