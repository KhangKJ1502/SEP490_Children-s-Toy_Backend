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
    private readonly DbSet<Wallet> _dbSet;

    public WalletRepository(SEP490ToyStoreContext context)
    {
        _context = context;
        _dbSet = context.Set<Wallet>();
    }

    public async Task<Wallet?> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(w => w.AccountId == accountId, cancellationToken);
    }

    public async Task<Wallet> AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        var result = await _dbSet.AddAsync(wallet, cancellationToken);
        return result.Entity;
    }

    public void Update(Wallet wallet)
    {
        _dbSet.Update(wallet);
    }
}
