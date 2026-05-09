using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class ShippingRepository : IShippingRepository
{
    private readonly SEP490ToyStoreContext _db;

    public ShippingRepository(SEP490ToyStoreContext db)
    {
        _db = db;
    }

    public async Task<ShippingProviderTransaction?> GetTransactionByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        return await _db.ShippingProviderTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.OrderId == orderId, ct);
    }
}
