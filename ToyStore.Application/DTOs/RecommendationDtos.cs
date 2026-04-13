using ToyStore.Domain.Enums;

namespace ToyStore.Application.DTOs;

/// <summary>
/// User behavior tracking DTO.
/// </summary>
public class UserBehaviorDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public BehaviorType BehaviorType { get; set; }
    public DateTime OccurredAt { get; set; }
    public int? DurationSeconds { get; set; }
}

/// <summary>
/// Request DTO for tracking user behavior.
/// </summary>
public class TrackBehaviorDto
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public BehaviorType BehaviorType { get; set; }
    public string? SessionId { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Metadata { get; set; }
}

/// <summary>
/// Product recommendation response DTO.
/// </summary>
public class RecommendationDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public string? ImageUrl { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public AgeRange AgeRange { get; set; }
    public decimal? AverageRating { get; set; }
    
    /// <summary>
    /// Recommendation score (0-100).
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// Reason for recommendation.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for getting recommendations.
/// </summary>
public class GetRecommendationsDto
{
    public Guid? UserId { get; set; }
    public AgeRange? AgeRange { get; set; }
    public List<ToyCategory>? Categories { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int Limit { get; set; } = 10;
}
