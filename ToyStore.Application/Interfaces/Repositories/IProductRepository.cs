using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository thao tac du lieu Product.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Lay danh sach Product co phan trang.
    /// </summary>
    Task<List<Product>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dem tong so Product theo dieu kien tim kiem.
    /// </summary>
    Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tim Product theo ID.
    /// </summary>
    Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tao moi Product.
    /// </summary>
    Task<Product> CreateAsync(
        Product product,
        IReadOnlyCollection<string>? additionalImageUrls = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat Product.
    /// </summary>
    Task<Product> UpdateAsync(
        Product product,
        IReadOnlyCollection<string>? additionalImageUrls = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh sach anh phu cua Product.
    /// </summary>
    Task<List<string>> GetAdditionalImageUrlsAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra Category ton tai.
    /// </summary>
    Task<bool> CategoryExistsAsync(short categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra Brand ton tai.
    /// </summary>
    Task<bool> BrandExistsAsync(short brandId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra PriceRange ton tai.
    /// </summary>
    Task<bool> PriceRangeExistsAsync(byte priceRangeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra Material ton tai.
    /// </summary>
    Task<bool> MaterialExistsAsync(short materialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra Age ton tai.
    /// </summary>
    Task<bool> AgeExistsAsync(byte ageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra Sex ton tai.
    /// </summary>
    Task<bool> SexExistsAsync(byte sexId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra Origin ton tai.
    /// </summary>
    Task<bool> OriginExistsAsync(byte originId, CancellationToken cancellationToken = default);
}
