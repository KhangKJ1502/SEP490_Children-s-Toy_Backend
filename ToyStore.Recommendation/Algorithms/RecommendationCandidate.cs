namespace ToyStore.Recommendation.Algorithms;

/// <summary>
/// 1 ứng viên gợi ý đi qua pipeline algorithm → business rules → enrich → response.
/// </summary>
public class RecommendationCandidate
{
    public int ProductId { get; set; }
    public decimal Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
}
