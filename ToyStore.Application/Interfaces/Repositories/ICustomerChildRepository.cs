using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface ICustomerChildRepository
{
    /// <summary>
    /// Lấy danh sách bé (chưa xóa) của một account.
    /// </summary>
    Task<List<CustomerChild>> GetActiveByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy một bé theo ChildId (chưa xóa).
    /// </summary>
    Task<CustomerChild?> GetActiveByIdAsync(int childId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm bé mới.
    /// </summary>
    Task AddAsync(CustomerChild child, CancellationToken cancellationToken = default);
}
