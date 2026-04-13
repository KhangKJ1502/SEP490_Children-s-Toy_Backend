using ToyStore.Application.DTOs;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service interface for recommendation engine.
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Gets personalized recommendations for a user.
    /// </summary>
    Task<IReadOnlyList<RecommendationDto>> GetPersonalizedRecommendationsAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets recommendations based on criteria.
    /// </summary>
    Task<IReadOnlyList<RecommendationDto>> GetRecommendationsAsync(
        GetRecommendationsDto request,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets similar products.
    /// </summary>
    Task<IReadOnlyList<RecommendationDto>> GetSimilarProductsAsync(
        Guid productId,
        int limit = 10,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets frequently bought together products.
    /// </summary>
    Task<IReadOnlyList<RecommendationDto>> GetFrequentlyBoughtTogetherAsync(
        Guid productId,
        int limit = 5,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tracks user behavior.
    /// </summary>
    Task TrackBehaviorAsync(
        TrackBehaviorDto behavior,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Recalculates recommendation scores.
    /// </summary>
    Task RecalculateRecommendationScoresAsync(CancellationToken cancellationToken = default);
}
