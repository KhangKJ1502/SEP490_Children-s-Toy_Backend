using Microsoft.EntityFrameworkCore;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Recommendation.Algorithms;

/// <summary>
/// Thuật toán Trending — đọc TrendingProducts theo scope:
///  - "global": top sản phẩm trending toàn site (homepage_trending).
///  - "category:{id}": top trending trong cùng category (fallback cho pdp_similar).
/// </summary>
public class TrendingAlgorithm
{
    private readonly SEP490ToyStoreContext _db;

    public TrendingAlgorithm(SEP490ToyStoreContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy top trending sản phẩm theo scope (mặc định: global). Trả về candidate đã sort.
    /// </summary>
    public async Task<List<RecommendationCandidate>> GetCandidatesAsync(
        string scope,
        int take,
        CancellationToken ct)
    {
        if (take <= 0) return new List<RecommendationCandidate>();
        if (string.IsNullOrWhiteSpace(scope)) scope = "global";

        // Lấy max 50 row vì TrendingProducts mỗi scope ≤ 20 (an toàn cao)
        var rows = await _db.TrendingProducts
            .AsNoTracking()
            .Where(t => t.Scope == scope)
            .OrderBy(t => t.Rank)
            .Take(Math.Max(take * 2, take + 10))
            .Select(t => new { t.ProductId, t.Score })
            .ToListAsync(ct);

        return rows.Select(r => new RecommendationCandidate
        {
            ProductId = r.ProductId,
            Score = r.Score,
            Reason = "Đang thịnh hành",
            ReasonCode = "trending",
        }).ToList();
    }

    /// <summary>
    /// Lấy trending fallback theo categoryId (nếu scope category không có, fallback global).
    /// </summary>
    public async Task<List<RecommendationCandidate>> GetCategoryOrGlobalAsync(
        short categoryId,
        int take,
        CancellationToken ct)
    {
        var categoryScope = $"category:{categoryId}";
        var byCategory = await GetCandidatesAsync(categoryScope, take, ct);
        if (byCategory.Count > 0) return byCategory;

        return await GetCandidatesAsync("global", take, ct);
    }
}
