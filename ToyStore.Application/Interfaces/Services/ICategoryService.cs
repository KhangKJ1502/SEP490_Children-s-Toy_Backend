using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Categories;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service quản lý Category.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Lấy danh sách Category có phân trang.
    /// </summary>
    Task<Result<PaginatedResponse<CategoryListDto>>> GetCategoriesAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin chi tiết Category theo ID.
    /// </summary>
    Task<Result<CategoryListDto>> GetCategoryByIdAsync(
        short categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới Category.
    /// </summary>
    Task<Result<CategoryListDto>> CreateCategoryAsync(
        CreateCategoryDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật Category.
    /// </summary>
    Task<Result<CategoryListDto>> UpdateCategoryAsync(
        short categoryId,
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm Category.
    /// </summary>
    Task<Result<PaginatedResponse<CategoryListDto>>> SearchCategoriesAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken cancellationToken = default);
}