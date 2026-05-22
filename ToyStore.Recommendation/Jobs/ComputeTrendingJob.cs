using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;
using ToyStore.Recommendation.Configuration;

namespace ToyStore.Recommendation.Jobs;

/// <summary>
/// Job C — Tính TrendingProducts (Popularity Model).
/// Mỗi 1 giờ:
///  - Đếm event 24h qua nhóm theo ProductID.
///  - Score = view × 1 + purchase × 5 + cart × 3.
///  - Lưu top 20 cho scope='global' và mỗi 'category:{id}'.
/// </summary>
public class ComputeTrendingJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly RecommendationOptions _options;
    private readonly ILogger<ComputeTrendingJob> _logger;

    public ComputeTrendingJob(
        IServiceProvider services,
        IOptions<RecommendationOptions> options,
        ILogger<ComputeTrendingJob> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ComputeTrendingJob starting — interval {Minutes} minutes, window {Hours}h, topK {TopK}",
            _options.ComputeTrendingIntervalMinutes, _options.TrendingWindowHours, _options.TrendingTopK);

        // Lệch 3 phút sau ComputeScores
        try { await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "ComputeTrendingJob iteration failed"); }

            try { await Task.Delay(TimeSpan.FromMinutes(_options.ComputeTrendingIntervalMinutes), stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();

        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(-_options.TrendingWindowHours);

        // 1. Lấy event 24h qua kèm CategoryId của product (Join lúc query để gọn)
        var rawEvents = await db.Events
            .AsNoTracking()
            .Where(e => e.CreatedAt >= cutoff && e.EntityType == "product")
            .Select(e => new { e.EntityId, e.EventType, e.CreatedAt })
            .ToListAsync(ct);

        if (rawEvents.Count == 0)
        {
            _logger.LogInformation("ComputeTrendingJob: no events in last {Hours}h", _options.TrendingWindowHours);
            return;
        }

        // 2. Group theo ProductId → count view/cart/purchase
        var byProduct = new Dictionary<int, TrendingAggregate>();
        foreach (var ev in rawEvents)
        {
            if (!int.TryParse(ev.EntityId, out var pid) || pid <= 0) continue;
            if (!byProduct.TryGetValue(pid, out var agg))
            {
                agg = new TrendingAggregate();
                byProduct[pid] = agg;
            }
            switch (ev.EventType.ToLowerInvariant())
            {
                case EventWeights.ProductView:
                case EventWeights.ProductViewLong:
                    agg.ViewCount++;
                    break;
                case EventWeights.AddToCart:
                    agg.CartCount++;
                    break;
                case EventWeights.Purchase:
                    agg.PurchaseCount++;
                    break;
            }
        }

        // 3. Tính score & loại product không Active (đảm bảo gợi ý hợp lệ ngay khi truy cập)
        var productIds = byProduct.Keys.ToList();
        var activeProducts = await db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.ProductId)
                     && !p.IsDeleted
                     && p.ProductStatus == "Active"
                     && p.Quantity > 0)
            .Select(p => new { p.ProductId, p.CategoryId })
            .ToListAsync(ct);

        var activeIds = activeProducts.ToDictionary(x => x.ProductId, x => x.CategoryId);

        var scored = byProduct
            .Where(kv => activeIds.ContainsKey(kv.Key))
            .Select(kv => new TrendingScore
            {
                ProductId = kv.Key,
                CategoryId = activeIds[kv.Key],
                Score = kv.Value.ViewCount * 1m + kv.Value.PurchaseCount * 5m + kv.Value.CartCount * 3m,
                ViewCount = kv.Value.ViewCount,
                PurchaseCount = kv.Value.PurchaseCount,
            })
            .Where(s => s.Score > 0m)
            .ToList();

        if (scored.Count == 0)
        {
            _logger.LogInformation("ComputeTrendingJob: no scored products");
            return;
        }

        // 4. Tạo data scope='global' top K
        var topK = Math.Max(1, _options.TrendingTopK);
        var windowHours = (byte)Math.Clamp(_options.TrendingWindowHours, 1, byte.MaxValue);

        var toInsert = new List<TrendingProduct>();
        var globalTop = scored.OrderByDescending(s => s.Score).Take(topK).ToList();
        AppendTrending(toInsert, "global", globalTop, now, windowHours);

        // 5. Tạo data scope='category:{categoryId}' top K cho mỗi category có sản phẩm
        var byCategory = scored.GroupBy(s => s.CategoryId);
        foreach (var grp in byCategory)
        {
            var top = grp.OrderByDescending(s => s.Score).Take(topK).ToList();
            AppendTrending(toInsert, $"category:{grp.Key}", top, now, windowHours);
        }

        // 6. Xoá data cũ + insert mới (TrendingProducts chỉ giữ snapshot mới nhất)
        await db.TrendingProducts.ExecuteDeleteAsync(ct);
        await db.TrendingProducts.AddRangeAsync(toInsert, ct);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ComputeTrendingJob done — global {Global}, categories {Cat}",
            globalTop.Count, byCategory.Count());
    }

    private static void AppendTrending(
        List<TrendingProduct> bag, string scope, List<TrendingScore> top, DateTime now, byte windowHours)
    {
        short rank = 1;
        foreach (var s in top)
        {
            bag.Add(new TrendingProduct
            {
                ProductId = s.ProductId,
                Scope = scope,
                Score = Math.Round(s.Score, 4, MidpointRounding.AwayFromZero),
                ViewCount = s.ViewCount,
                PurchaseCount = s.PurchaseCount,
                Rank = rank++,
                WindowHours = windowHours,
                ComputedAt = now,
            });
        }
    }

    private sealed class TrendingAggregate
    {
        public int ViewCount;
        public int CartCount;
        public int PurchaseCount;
    }

    private sealed class TrendingScore
    {
        public int ProductId;
        public short CategoryId;
        public decimal Score;
        public int ViewCount;
        public int PurchaseCount;
    }
}
