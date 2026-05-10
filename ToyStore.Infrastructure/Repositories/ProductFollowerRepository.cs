using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class ProductFollowerRepository : IProductFollowerRepository
{
    private readonly SEP490ToyStoreContext _dbContext;

    public ProductFollowerRepository(SEP490ToyStoreContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Lấy danh sách người theo dõi theo AccountId.
    /// </summary>
    public async Task<List<ProductFollower>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductFollowers
            .Where(x => x.AccountId == accountId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy thông tin theo dõi theo AccountId và ProductId.
    /// </summary>
    public async Task<ProductFollower?> GetByAccountAndProductAsync(int accountId, int productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductFollowers
            .FirstOrDefaultAsync(x => x.AccountId == accountId && x.ProductId == productId, cancellationToken);
    }

    /// <summary>
    /// Thêm người theo dõi mới.
    /// </summary>
    public async Task AddAsync(ProductFollower follower, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProductFollowers.AddAsync(follower, cancellationToken);
    }

    /// <summary>
    /// Xóa người theo dõi.
    /// </summary>
    public void Remove(ProductFollower follower)
    {
        _dbContext.ProductFollowers.Remove(follower);
    }
}
