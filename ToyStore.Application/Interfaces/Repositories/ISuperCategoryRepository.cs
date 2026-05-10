using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository thao tác dữ liệu SuperCategory.
/// </summary>
public interface ISuperCategoryRepository
{
    /// <summary>
    /// Lấy danh sách SuperCategory có phân trang.
    /// </summary>
    Task<List<SuperCategory>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm tổng số SuperCategory theo điều kiện tìm kiếm.
    /// </summary>
    Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra SuperCategory name đã tồn tại chưa.
    /// </summary>
    Task<bool> ExistsByNameAsync(string superCategoryName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra SuperCategory name đã tồn tại chưa, ngoại trừ một ID.
    /// </summary>
    Task<bool> ExistsByNameExceptIdAsync(
        string superCategoryName,
        short superCategoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm SuperCategory theo ID.
    /// </summary>
    Task<SuperCategory?> GetByIdAsync(short superCategoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới SuperCategory.
    /// </summary>
    Task<SuperCategory> CreateAsync(string superCategoryName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật SuperCategory.
    /// </summary>
    Task<SuperCategory> UpdateAsync(
        short superCategoryId,
        string superCategoryName,
        bool? isDeleted = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật trạng thái của toàn bộ Category/Product thuộc SuperCategory.
    /// </summary>
    Task UpdateRelatedStatusAsync(
        short superCategoryId,
        bool isDeleted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả SuperCategory (không phân trang).
    /// </summary>
    Task<List<SuperCategory>> GetAllAsync(CancellationToken cancellationToken = default);
}