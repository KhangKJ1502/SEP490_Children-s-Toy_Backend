namespace ToyStore.Recommendation.Models;

/// <summary>
/// Model for calculating recommendation scores.
/// </summary>
public class RecommendationScore
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Individual score components for transparency.
    /// </summary>
    public ScoreComponents Components { get; set; } = new();
}

/// <summary>
/// Breakdown of score components.
/// </summary>
public class ScoreComponents
{
    /// <summary>
    /// Score from user's purchase history (0-25).
    /// </summary>
    public double PurchaseHistoryScore { get; set; }
    
    /// <summary>
    /// Score from user's view history (0-20).
    /// </summary>
    public double ViewHistoryScore { get; set; }
    
    /// <summary>
    /// Score from category preference match (0-20).
    /// </summary>
    public double CategoryMatchScore { get; set; }
    
    /// <summary>
    /// Score from age range match (0-15).
    /// </summary>
    public double AgeRangeMatchScore { get; set; }
    
    /// <summary>
    /// Score from product popularity (0-10).
    /// </summary>
    public double PopularityScore { get; set; }
    
    /// <summary>
    /// Score from product rating (0-10).
    /// </summary>
    public double RatingScore { get; set; }
    
    /// <summary>
    /// Calculates total score (max 100).
    /// </summary>
    public double CalculateTotal()
    {
        return PurchaseHistoryScore + ViewHistoryScore + CategoryMatchScore + 
               AgeRangeMatchScore + PopularityScore + RatingScore;
    }
}
