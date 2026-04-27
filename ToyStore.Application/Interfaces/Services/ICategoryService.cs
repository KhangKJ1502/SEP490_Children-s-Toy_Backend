using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Categories;
using ToyStore.Application.DTOs.SuperCategories;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service quản lý Category và SuperCategory.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Lấy danh sách SuperCategory có phân trang.
    /// </summary>
    Task<Result<PaginatedResponse<SuperCategoryListDto>>> GetSuperCategoriesAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới SuperCategory.
    /// </summary>
    Task<Result<SuperCategoryListDto>> CreateSuperCategoryAsync(
        CreateSuperCategoryDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật SuperCategory.
    /// </summary>
    Task<Result<SuperCategoryListDto>> UpdateSuperCategoryAsync(
        short superCategoryId,
        UpdateSuperCategoryDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm SuperCategory.
    /// </summary>
    Task<Result<PaginatedResponse<SuperCategoryListDto>>> SearchSuperCategoriesAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken cancellationToken = default);

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