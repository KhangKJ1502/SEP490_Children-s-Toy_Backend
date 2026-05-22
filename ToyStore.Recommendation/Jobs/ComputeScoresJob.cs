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
/// Job A — Tính UserProductScores (Weighted Scoring).
/// Mỗi 1 giờ:
///  - Đọc Events 30 ngày gần nhất.
///  - Với mỗi (AccountID, ProductID): Score = SUM(weight × e^(-0.1 × daysAgo)).
///  - Upsert vào Recommendation.UserProductScores (cộng cả counter views/cart/purchase/wishlist).
/// </summary>
public class ComputeScoresJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly RecommendationOptions _options;
    private readonly ILogger<ComputeScoresJob> _logger;

    public ComputeScoresJob(
        IServiceProvider services,
        IOptions<RecommendationOptions> options,
        ILogger<ComputeScoresJob> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ComputeScoresJob starting — interval {Minutes} minutes, window {Days} days",
            _options.ComputeScoresIntervalMinutes, _options.ScoreWindowDays);

        // Lệch khởi động (1 phút) so với FlushEventsJob để dữ liệu kịp về SQL Server
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "ComputeScoresJob iteration failed"); }

            try { await Task.Delay(TimeSpan.FromMinutes(_options.ComputeScoresIntervalMinutes), stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();

        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-_options.ScoreWindowDays);

        // 1. Lấy tất cả event có AccountId (loại guest) và EntityType = product trong cửa sổ 30 ngày
        //    (Score chỉ tính khi liên kết được account ↔ product)
        var rawEvents = await db.Events
            .AsNoTracking()
            .Where(e => e.CreatedAt >= cutoff
                     && e.AccountId != null
                     && e.EntityType == "product")
            .Select(e => new
            {
                e.AccountId,
                e.EntityId,
                e.EventType,
                e.CreatedAt,
            })
            .ToListAsync(ct);

        if (rawEvents.Count == 0)
        {
            _logger.LogInformation("ComputeScoresJob: no events in last {Days} days", _options.ScoreWindowDays);
            return;
        }

        // 2. Gom theo (AccountId, ProductId) → tính score + counter
        var aggregates = new Dictionary<(int AccountId, int ProductId), ScoreAggregate>();
        foreach (var ev in rawEvents)
        {
            if (!int.TryParse(ev.EntityId, out var productId) || productId <= 0) continue;
            var accountId = ev.AccountId!.Value;
            if (accountId <= 0) continue;

            var baseWeight = EventWeights.GetWeight(ev.EventType);
            if (baseWeight == 0m) continue; // bỏ qua event không có weight

            var daysAgo = (now - ev.CreatedAt).TotalDays;
            var weighted = EventWeights.ApplyTimeDecay(baseWeight, daysAgo);

            var key = (accountId, productId);
            if (!aggregates.TryGetValue(key, out var agg))
            {
                agg = new ScoreAggregate();
                aggregates[key] = agg;
            }
            agg.Score += weighted;
            agg.LastInteractedAt = ev.CreatedAt > agg.LastInteractedAt ? ev.CreatedAt : agg.LastInteractedAt;

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
                case EventWeights.AddToWishlist:
                    agg.WishlistCount++;
                    break;
            }
        }

        if (aggregates.Count == 0)
        {
            _logger.LogInformation("ComputeScoresJob: no scored aggregates produced");
            return;
        }

        // 3. Upsert vào Recommendation.UserProductScores
        //    Strategy: load existing trong batch → update / insert → SaveChanges 1 lần
        var accountIds = aggregates.Keys.Select(k => k.AccountId).Distinct().ToList();
        var productIds = aggregates.Keys.Select(k => k.ProductId).Distinct().ToList();

        var existing = await db.UserProductScores
            .Where(x => accountIds.Contains(x.AccountId) && productIds.Contains(x.ProductId))
            .ToListAsync(ct);

        var existingMap = existing.ToDictionary(x => (x.AccountId, x.ProductId));

        var toAdd = new List<UserProductScore>();
        foreach (var ((accId, prodId), agg) in aggregates)
        {
            // Cap counters về byte/short để không tràn datatype (CartCount byte → 255)
            var viewCount = (short)Math.Min(agg.ViewCount, short.MaxValue);
            var cartCount = (byte)Math.Min(agg.CartCount, byte.MaxValue);
            var purchaseCount = (byte)Math.Min(agg.PurchaseCount, byte.MaxValue);
            var wishlistCount = (byte)Math.Min(agg.WishlistCount, byte.MaxValue);
            // Round score 4 chữ số (decimal đủ chứa)
            var score = Math.Round(agg.Score, 4, MidpointRounding.AwayFromZero);

            if (existingMap.TryGetValue((accId, prodId), out var entity))
            {
                entity.Score = score;
                entity.ViewCount = viewCount;
                entity.CartCount = cartCount;
                entity.PurchaseCount = purchaseCount;
                entity.WishlistCount = wishlistCount;
                entity.LastInteractedAt = agg.LastInteractedAt;
                entity.ComputedAt = now;
            }
            else
            {
                toAdd.Add(new UserProductScore
                {
                    AccountId = accId,
                    ProductId = prodId,
                    Score = score,
                    ViewCount = viewCount,
                    CartCount = cartCount,
                    PurchaseCount = purchaseCount,
                    WishlistCount = wishlistCount,
                    LastInteractedAt = agg.LastInteractedAt,
                    ComputedAt = now,
                });
            }
        }

        if (toAdd.Count > 0)
        {
            await db.UserProductScores.AddRangeAsync(toAdd, ct);
        }

        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ComputeScoresJob done — {Existing} updated, {New} inserted (total {Total} pairs)",
            existing.Count, toAdd.Count, aggregates.Count);
    }

    private sealed class ScoreAggregate
    {
        public decimal Score;
        public int ViewCount;
        public int CartCount;
        public int PurchaseCount;
        public int WishlistCount;
        public DateTime LastInteractedAt;
    }
}
