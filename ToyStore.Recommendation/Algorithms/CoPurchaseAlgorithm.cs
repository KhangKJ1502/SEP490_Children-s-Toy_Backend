using Microsoft.EntityFrameworkCore;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Recommendation.Algorithms;

/// <summary>
/// Thuật toán Collaborative Filtering theo "co-purchase":
///  - Với mỗi sản phẩm X, tìm các order có chứa X, lấy các product khác trong cùng order.
///  - Đếm tần suất → top sản phẩm thường được mua kèm.
/// Áp dụng cho widget pdp_also_bought và after_purchase.
/// </summary>
public class CoPurchaseAlgorithm
{
    private readonly SEP490ToyStoreContext _db;

    public CoPurchaseAlgorithm(SEP490ToyStoreContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy danh sách product thường được mua kèm với sourceProductId.
    /// </summary>
    public async Task<List<RecommendationCandidate>> GetCandidatesAsync(
        int sourceProductId,
        int take,
        CancellationToken ct)
    {
        if (sourceProductId <= 0 || take <= 0) return new List<RecommendationCandidate>();

        // 1. Tìm các OrderId có chứa sourceProductId (giới hạn 1000 order gần đây nhất → tránh OOM)
        var orderIds = await _db.OrderDetails
            .AsNoTracking()
            .Where(od => od.ProductId == sourceProductId
                      && !od.Order.IsDeleted
                      && od.Order.CancelledAt == null)
            .OrderByDescending(od => od.Order.OrderDate)
            .Select(od => od.OrderId)
            .Take(1000)
            .ToListAsync(ct);

        if (orderIds.Count == 0) return new List<RecommendationCandidate>();

        // 2. Lấy các product khác xuất hiện trong các order đó, group + đếm
        var coPurchases = await _db.OrderDetails
            .AsNoTracking()
            .Where(od => orderIds.Contains(od.OrderId)
                      && od.ProductId != sourceProductId
                      && !od.Order.IsDeleted
                      && od.Order.CancelledAt == null)
            .GroupBy(od => od.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                CoPurchaseCount = g.Count(),
            })
            .OrderByDescending(x => x.CoPurchaseCount)
            .Take(Math.Max(take * 2, take + 10))
            .ToListAsync(ct);

        if (coPurchases.Count == 0) return new List<RecommendationCandidate>();

        // 3. Map sang candidate — score = số lần mua kèm (chuẩn hoá nhẹ)
        var max = coPurchases.Max(x => x.CoPurchaseCount);
        return coPurchases.Select(c => new RecommendationCandidate
        {
            ProductId = c.ProductId,
            // Score: tỉ lệ với max để 0 < score ≤ 1, nhân 5 cho phép so sánh với trending
            Score = max == 0 ? 0 : Math.Round((decimal)c.CoPurchaseCount / max * 5m, 4),
            Reason = "Khách hàng cũng mua",
            ReasonCode = "co_purchase",
        }).ToList();
    }

    /// <summary>
    /// Lấy gợi ý co-purchase dựa trên toàn bộ sản phẩm của 1 order (multi-seed).
    /// Dùng cho after_purchase để không bị lệch theo chỉ 1 sản phẩm đầu tiên.
    /// </summary>
    public async Task<List<RecommendationCandidate>> GetCandidatesByOrderAsync(
        int orderId,
        int take,
        CancellationToken ct)
    {
        if (orderId <= 0 || take <= 0) return new List<RecommendationCandidate>();

        var seedProductIds = await _db.OrderDetails
            .AsNoTracking()
            .Where(od => od.OrderId == orderId
                      && !od.Order.IsDeleted
                      && od.Order.CancelledAt == null)
            .Select(od => od.ProductId)
            .Distinct()
            .ToListAsync(ct);

        if (seedProductIds.Count == 0) return new List<RecommendationCandidate>();

        var relatedOrderIds = await _db.OrderDetails
            .AsNoTracking()
            .Where(od => seedProductIds.Contains(od.ProductId)
                      && !od.Order.IsDeleted
                      && od.Order.CancelledAt == null)
            .OrderByDescending(od => od.Order.OrderDate)
            .Select(od => od.OrderId)
            .Distinct()
            .Take(1000)
            .ToListAsync(ct);

        if (relatedOrderIds.Count == 0) return new List<RecommendationCandidate>();

        var coPurchases = await _db.OrderDetails
            .AsNoTracking()
            .Where(od => relatedOrderIds.Contains(od.OrderId)
                      && !seedProductIds.Contains(od.ProductId)
                      && !od.Order.IsDeleted
                      && od.Order.CancelledAt == null)
            .GroupBy(od => od.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                CoPurchaseCount = g.Count(),
            })
            .OrderByDescending(x => x.CoPurchaseCount)
            .Take(Math.Max(take * 2, take + 10))
            .ToListAsync(ct);

        if (coPurchases.Count == 0) return new List<RecommendationCandidate>();

        var max = coPurchases.Max(x => x.CoPurchaseCount);
        return coPurchases.Select(c => new RecommendationCandidate
        {
            ProductId = c.ProductId,
            Score = max == 0 ? 0 : Math.Round((decimal)c.CoPurchaseCount / max * 5m, 4),
            Reason = "Mua tiếp theo",
            ReasonCode = "co_purchase_order",
        }).ToList();
    }
}
