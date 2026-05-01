using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository xử lý truy vấn dữ liệu promotion.
/// </summary>
public interface IPromotionRepository
{
    /// <summary>
    /// Lấy danh sách promotion có phân trang và tìm kiếm.
    /// </summary>
    Task<PaginatedResponse<Promotion>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy promotion theo ID.
    /// </summary>
    Task<Promotion?> GetByIdAsync(int promotionId, CancellationToken cancellationToken = default, string? includeProperties = null);

    /// <summary>
    /// Kiểm tra promotion name đã tồn tại hay chưa.
    /// </summary>
    Task<bool> ExistsPromotionNameAsync(
        string promotionName,
        int? excludePromotionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm promotion mới vào context.
    /// </summary>
    Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật promotion vào context.
    /// </summary>
    void Update(Promotion promotion);
}
