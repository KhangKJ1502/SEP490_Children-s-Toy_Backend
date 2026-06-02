using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Recommendations;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service phục vụ gợi ý real-time cho FE.
/// Bao gồm 4 thuật toán: Content-Based, Weighted Scoring, Collaborative, Trending.
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Lấy danh sách gợi ý cho 1 widget cụ thể.
    /// </summary>
    /// <param name="widgetCode">Mã widget cấu hình trong DB.Recommendation.Widgets.</param>
    /// <param name="accountId">AccountId người dùng (nullable cho guest).</param>
    /// <param name="productId">ProductId — bắt buộc cho pdp_similar / pdp_also_bought.</param>
    Task<Result<RecommendationWidgetResponseDto>> GetRecommendationsAsync(
        string widgetCode,
        int? accountId,
        int? productId,
        int? orderId,
        CancellationToken cancellationToken = default);
}
