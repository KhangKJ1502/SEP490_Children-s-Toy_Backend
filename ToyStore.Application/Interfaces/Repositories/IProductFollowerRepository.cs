using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IProductFollowerRepository
{
    /// <summary>
    /// Lấy danh sách người theo dõi theo AccountId.
    /// </summary>
    Task<List<ProductFollower>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin theo dõi theo AccountId và ProductId.
    /// </summary>
    Task<ProductFollower?> GetByAccountAndProductAsync(int accountId, int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm người theo dõi mới.
    /// </summary>
    Task AddAsync(ProductFollower follower, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa người theo dõi.
    /// </summary>
    void Remove(ProductFollower follower);
}
