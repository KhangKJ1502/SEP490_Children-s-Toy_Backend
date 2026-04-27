using ToyStore.Application.Common.Models;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IBrandRepository
{
    Task<List<BrandModel>> GetPagedAsync(
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

    Task<BrandModel?> GetByIdAsync(short brandId, CancellationToken cancellationToken = default);

    Task<BrandModel> CreateAsync(string brandName, CancellationToken cancellationToken = default);

    Task<BrandModel> UpdateAsync(
        short brandId,
        string brandName,
        CancellationToken cancellationToken = default);
}
