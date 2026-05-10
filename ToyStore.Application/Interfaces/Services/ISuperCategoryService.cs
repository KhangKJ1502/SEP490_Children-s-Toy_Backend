using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.SuperCategories;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service quan ly SuperCategory.
/// </summary>
public interface ISuperCategoryService
{
    /// <summary>
    /// Lay danh sach SuperCategory co phan trang.
    /// </summary>
    Task<Result<PaginatedResponse<SuperCategoryListDto>>> GetSuperCategoriesAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay thong tin chi tiet SuperCategory theo ID.
    /// </summary>
    Task<Result<SuperCategoryListDto>> GetSuperCategoryByIdAsync(
        short superCategoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tao moi SuperCategory.
    /// </summary>
    Task<Result<SuperCategoryListDto>> CreateSuperCategoryAsync(
        CreateSuperCategoryDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat SuperCategory.
    /// </summary>
    Task<Result<SuperCategoryListDto>> UpdateSuperCategoryAsync(
        short superCategoryId,
        UpdateSuperCategoryDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tim kiem SuperCategory.
    /// </summary>
    Task<Result<PaginatedResponse<SuperCategoryListDto>>> SearchSuperCategoriesAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken cancellationToken = default);
}
