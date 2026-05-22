using Microsoft.EntityFrameworkCore;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Recommendation.Algorithms;

/// <summary>
/// Thuật toán Weighted Scoring (Behavior-Based Ranking).
/// - Lấy top UserProductScores của 1 account (đã được job tính SUM(weight × decay)).
/// - Optionally mở rộng bằng ItemSimilarities (sản phẩm tương tự với những món đã thích).
/// Dùng cho widget homepage_personal.
/// </summary>
public class WeightedScoringAlgorithm
{
    private readonly SEP490ToyStoreContext _db;

    public WeightedScoringAlgorithm(SEP490ToyStoreContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy candidate cá nhân hoá cho 1 account, có mở rộng bằng similarity.
    /// </summary>
    public async Task<List<RecommendationCandidate>> GetCandidatesAsync(
        int accountId,
        int take,
        CancellationToken ct)
    {
        if (accountId <= 0 || take <= 0) return new List<RecommendationCandidate>();

        // 1. Top score user trực tiếp (lấy gấp 2 lần để có buffer cho business rules filter)
        var directTake = Math.Max(take * 2, take + 10);
        var direct = await _db.UserProductScores
            .AsNoTracking()
            .Where(x => x.AccountId == accountId && x.Score > 0m)
            .OrderByDescending(x => x.Score)
            .Take(directTake)
            .Select(x => new { x.ProductId, x.Score })
            .ToListAsync(ct);

        var candidates = direct.Select(d => new RecommendationCandidate
        {
            ProductId = d.ProductId,
            Score = d.Score,
            Reason = "Dành cho bạn",
            ReasonCode = "weighted_score",
        }).ToList();

        // 2. Mở rộng bằng ItemSimilarities — với mỗi product user thích, lấy thêm 3 similar
        //    Chỉ lấy top 5 sản phẩm "thích" để giới hạn số query
        var seedIds = direct.Take(5).Select(d => d.ProductId).ToList();
        if (seedIds.Count > 0)
        {
            var existingIds = candidates.Select(c => c.ProductId).ToHashSet();

            var related = await _db.ItemSimilarities
                .AsNoTracking()
                .Where(s => seedIds.Contains(s.SourceProductId) && s.AlgorithmType == "content_based")
                .OrderByDescending(s => s.SimilarityScore)
                .Take(seedIds.Count * 3)
                .Select(s => new { s.SimilarProductId, s.SimilarityScore })
                .ToListAsync(ct);

            foreach (var r in related)
            {
                if (existingIds.Contains(r.SimilarProductId)) continue;
                candidates.Add(new RecommendationCandidate
                {
                    ProductId = r.SimilarProductId,
                    // Similarity score thường < 1 → nhân lên để comparable với weighted score
                    Score = Math.Round(r.SimilarityScore * 2m, 4),
                    Reason = "Có thể bạn quan tâm",
                    ReasonCode = "weighted_score_expand",
                });
                existingIds.Add(r.SimilarProductId);
            }
        }

        // 3. Sort theo score giảm dần
        return candidates.OrderByDescending(c => c.Score).ToList();
    }
}
