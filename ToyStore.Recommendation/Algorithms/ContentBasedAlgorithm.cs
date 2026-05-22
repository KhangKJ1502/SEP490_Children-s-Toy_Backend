using Microsoft.EntityFrameworkCore;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Recommendation.Algorithms;

/// <summary>
/// Thuật toán Content-Based — đọc ItemSimilarities cho product nguồn,
/// trả về các sản phẩm có feature gần giống nhất (đã pre-compute bởi job).
/// </summary>
public class ContentBasedAlgorithm
{
    private readonly SEP490ToyStoreContext _db;

    public ContentBasedAlgorithm(SEP490ToyStoreContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy danh sách candidate có nội dung tương tự với sourceProductId (đã sort theo similarity).
    /// </summary>
    public async Task<List<RecommendationCandidate>> GetCandidatesAsync(
        int sourceProductId,
        int take,
        CancellationToken ct)
    {
        if (sourceProductId <= 0 || take <= 0) return new List<RecommendationCandidate>();

        var rows = await _db.ItemSimilarities
            .AsNoTracking()
            .Where(x => x.SourceProductId == sourceProductId && x.AlgorithmType == "content_based")
            .OrderByDescending(x => x.SimilarityScore)
            .Take(Math.Max(take * 2, take + 5))
            .Select(x => new { x.SimilarProductId, x.SimilarityScore })
            .ToListAsync(ct);

        return rows.Select(r => new RecommendationCandidate
        {
            ProductId = r.SimilarProductId,
            Score = r.SimilarityScore,
            Reason = "Sản phẩm tương tự",
            ReasonCode = "content_based",
        }).ToList();
    }
}
