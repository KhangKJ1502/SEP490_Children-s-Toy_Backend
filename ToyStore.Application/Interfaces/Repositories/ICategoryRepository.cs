using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository thao tác dữ liệu Category.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Lấy danh sách Category có phân trang.
    /// </summary>
    Task<List<Category>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm tổng số Category theo điều kiện tìm kiếm.
    /// </summary>
    Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra Category name đã tồn tại chưa.
    /// </summary>
    Task<bool> ExistsByNameAsync(string categoryName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra Category name đã tồn tại chưa, ngoại trừ một ID.
    /// </summary>
    Task<bool> ExistsByNameExceptIdAsync(
        string categoryName,
        short categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm Category theo ID.
    /// </summary>
    Task<Category?> GetByIdAsync(short categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới Category.
    /// </summary>
    Task<Category> CreateAsync(
        short superCategoryId,
        string categoryName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật Category.
    /// </summary>
    Task<Category> UpdateAsync(
        short categoryId,
        short superCategoryId,
        string categoryName,
        CancellationToken cancellationToken = default);
}