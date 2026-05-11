using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IBrandRepository
{
    Task<List<Brand>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string brandName, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameExceptIdAsync(
        string brandName,
        short brandId,
        CancellationToken cancellationToken = default);

    Task<Brand?> GetByIdAsync(short brandId, CancellationToken cancellationToken = default);

    Task<Brand> CreateAsync(string brandName, CancellationToken cancellationToken = default);

    Task<Brand> UpdateAsync(
        short brandId,
        string brandName,
        bool isDeleted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả Brand (không phân trang).
    /// </summary>
    Task<List<Brand>> GetAllAsync(CancellationToken cancellationToken = default);
}
