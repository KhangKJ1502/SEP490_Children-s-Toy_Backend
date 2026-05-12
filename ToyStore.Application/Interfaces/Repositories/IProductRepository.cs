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
        short? superCategoryId = null,
        short? categoryId = null,
        IReadOnlyCollection<short>? categoryIds = null,
        IReadOnlyCollection<int>? brandIds = null,
        IReadOnlyCollection<byte>? priceRangeIds = null,
        IReadOnlyCollection<short>? materialIds = null,
        IReadOnlyCollection<byte>? ageIds = null,
        IReadOnlyCollection<byte>? sexIds = null,
        IReadOnlyCollection<byte>? originIds = null,
        int? rating = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dem tong so Product theo dieu kien tim kiem.
    /// </summary>
    Task<int> CountAsync(
        string? searchTerm = null,
        short? superCategoryId = null,
        short? categoryId = null,
        IReadOnlyCollection<short>? categoryIds = null,
        IReadOnlyCollection<int>? brandIds = null,
        IReadOnlyCollection<byte>? priceRangeIds = null,
        IReadOnlyCollection<short>? materialIds = null,
        IReadOnlyCollection<byte>? ageIds = null,
        IReadOnlyCollection<byte>? sexIds = null,
        IReadOnlyCollection<byte>? originIds = null,
        int? rating = null,
        string? status = null,
        CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Lay danh sach Product theo tap hop ID.
    /// </summary>
    Task<List<Product>> GetByIdsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh sach PriceRange.
    /// </summary>
    Task<List<PriceRange>> GetPriceRangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh sach Material.
    /// </summary>
    Task<List<Material>> GetMaterialsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh sach Age.
    /// </summary>
    Task<List<Age>> GetAgesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh sach Sex.
    /// </summary>
    Task<List<Sex>> GetSexesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh sach Origin.
    /// </summary>
    Task<List<Origin>> GetOriginsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat ton kho atomic (thread-safe).
    /// </summary>
    /// <param name="productId">ID san pham.</param>
    /// <param name="amount">So luong can thay doi (duong de tang, am de giam).</param>
    Task AdjustStockAsync(int productId, int amount, CancellationToken cancellationToken = default);
}
