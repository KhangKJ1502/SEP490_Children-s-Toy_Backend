using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Promotions;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service xử lý nghiệp vụ promotion.
/// </summary>
public interface IPromotionService
{
    /// <summary>
    /// Lấy danh sách promotion có phân trang, sắp xếp và tìm kiếm.
    /// </summary>
    Task<Result<PaginatedResponse<PromotionListDto>>> GetPromotionsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin promotion theo ID.
    /// </summary>
    Task<Result<PromotionDto>> GetPromotionByIdAsync(
        int promotionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới promotion.
    /// </summary>
    Task<Result<PromotionDto>> CreatePromotionAsync(
        CreatePromotionDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật promotion.
    /// </summary>
    Task<Result<PromotionDto>> UpdatePromotionAsync(
        int promotionId,
        UpdatePromotionDto request,
        CancellationToken cancellationToken = default);
}
