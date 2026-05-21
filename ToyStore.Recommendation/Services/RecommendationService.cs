using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Recommendations;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Recommendation.Algorithms;
using ToyStore.Recommendation.Configuration;
using ToyStore.Recommendation.MongoDb;
using ToyStore.Recommendation.MongoDb.Documents;

namespace ToyStore.Recommendation.Services;

/// <summary>
/// Orchestrator chọn thuật toán dựa trên widgetCode (đọc cấu hình Widget từ DB).
/// Pipeline:
///  Bước 1 — Check cache (MongoDB.recommendation_cache) theo key (widget+account+product).
///  Bước 2 — Chọn algorithm + lấy candidate (fallback Trending nếu rỗng).
///  Bước 3 — BusinessRulesFilter loại sp không hợp lệ + enrich data.
///  Bước 4 — Lưu cache (TTL 1 giờ).
///  Bước 5 — Return RecommendationWidgetResponseDto.
/// </summary>
public class RecommendationService : IRecommendationService
{
    private readonly SEP490ToyStoreContext _db;
    private readonly MongoDbContext _mongo;
    private readonly TrendingAlgorithm _trending;
    private readonly ContentBasedAlgorithm _contentBased;
    private readonly CoPurchaseAlgorithm _coPurchase;
    private readonly WeightedScoringAlgorithm _weighted;
    private readonly BusinessRulesFilter _filter;
    private readonly RecommendationOptions _options;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        SEP490ToyStoreContext db,
        MongoDbContext mongo,
        TrendingAlgorithm trending,
        ContentBasedAlgorithm contentBased,
        CoPurchaseAlgorithm coPurchase,
        WeightedScoringAlgorithm weighted,
        BusinessRulesFilter filter,
        IOptions<RecommendationOptions> options,
        ILogger<RecommendationService> logger)
    {
        _db = db;
        _mongo = mongo;
        _trending = trending;
        _contentBased = contentBased;
        _coPurchase = coPurchase;
        _weighted = weighted;
        _filter = filter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<RecommendationWidgetResponseDto>> GetRecommendationsAsync(
        string widgetCode,
        int? accountId,
        int? productId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(widgetCode))
            return Result<RecommendationWidgetResponseDto>.Failure("VALIDATION_ERROR", "widgetCode is required.");

        // ── 0. Load Widget config từ DB
        var widget = await _db.Widgets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WidgetCode == widgetCode && w.IsActive, ct);

        if (widget == null)
        {
            return Result<RecommendationWidgetResponseDto>.NotFound("Widget", widgetCode);
        }

        var maxItems = widget.MaxItems > 0 ? widget.MaxItems : (byte)10;

        // Xác định widget có phải public (KHÔNG filter "đã mua") hay không
        var isPublicWidget = widget.WidgetCode == "homepage_trending" 
                          || widget.WidgetCode == "pdp_similar" 
                          || widget.WidgetCode == "pdp_also_bought"
                          || widget.WidgetCode == "after_purchase";

        // ── 1. Cache lookup
        var cacheKey = BuildCacheKey(widgetCode, accountId, productId);
        var cached = await GetCachedAsync(cacheKey, ct);
        if (cached != null)
        {
            var cachedItems = await MapCachedItemsAsync(cached, isPublicWidget, ct);
            if (cachedItems.Count > 0)
            {
                return Result<RecommendationWidgetResponseDto>.Success(new RecommendationWidgetResponseDto
                {
                    WidgetCode = widget.WidgetCode,
                    WidgetName = widget.WidgetName,
                    Algorithm = widget.Algorithm,
                    Items = cachedItems,
                });
            }
            // Nếu enrich từ cache không còn item nào hợp lệ (sp đã ngừng bán) → tiếp tục chạy thuật toán bên dưới
        }

        // ── 2. Chạy thuật toán chính theo widget.Algorithm
        var candidates = await RunAlgorithmAsync(widget.Algorithm, accountId, productId, maxItems, ct);
        _logger.LogInformation(
            "Widget {Code} algorithm {Algo} returned {Count} candidates",
            widget.WidgetCode, widget.Algorithm, candidates.Count);

        // ── 2.5. Lấy user profile (cho business rules: purchased filter)
        UserProfileDocument? userProfile = null;
        if (accountId.HasValue && accountId.Value > 0)
        {
            try
            {
                userProfile = await _mongo.UserProfiles
                    .Find(x => x.AccountId == accountId.Value)
                    .FirstOrDefaultAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load user profile for AccountId={AccountId}", accountId.Value);
            }
        }

        // ── 3. Business rules filter + enrich
        // Chỉ apply "purchased filter" cho widget cá nhân hóa (homepage_personal)
        // KHÔNG apply cho widget công khai (trending, pdp_similar, pdp_also_bought, after_purchase)
        var items = await _filter.FilterAndEnrichAsync(candidates, maxItems, userProfile, ct, skipPurchasedFilter: isPublicWidget);
        _logger.LogInformation(
            "Widget {Code} after filter + enrich: {Count} items",
            widget.WidgetCode, items.Count);

        // ── 4. Fallback chain — nếu rỗng, dùng fallback algorithm
        if (items.Count == 0 && !string.IsNullOrWhiteSpace(widget.FallbackAlgo))
        {
            _logger.LogInformation(
                "Recommendation widget {Code} primary algo returned 0 items, falling back to {Fallback}",
                widget.WidgetCode, widget.FallbackAlgo);

            var fallbackCandidates = await RunFallbackAsync(
                widget.FallbackAlgo!, accountId, productId, maxItems, ct);
            items = await _filter.FilterAndEnrichAsync(fallbackCandidates, maxItems, userProfile, ct, skipPurchasedFilter: isPublicWidget);
        }

        // ── 4b. Last resort — Trending global (đảm bảo không bao giờ trả rỗng cho widget homepage_trending,
        //    pdp_similar, pdp_also_bought theo spec: "Cache miss phải fallback xuống trending, không được return rỗng")
        if (items.Count == 0)
        {
            var trendingCandidates = await _trending.GetCandidatesAsync("global", maxItems, ct);
            items = await _filter.FilterAndEnrichAsync(trendingCandidates, maxItems, userProfile, ct, skipPurchasedFilter: true);
        }

        // ── 5. Save cache (best-effort)
        if (items.Count > 0)
        {
            await SaveCacheAsync(cacheKey, widgetCode, accountId, productId, items, ct);
        }

        return Result<RecommendationWidgetResponseDto>.Success(new RecommendationWidgetResponseDto
        {
            WidgetCode = widget.WidgetCode,
            WidgetName = widget.WidgetName,
            Algorithm = widget.Algorithm,
            Items = items,
        });
    }

    /// <summary>Chọn algorithm theo tên (column Algorithm trong Widgets).</summary>
    private Task<List<RecommendationCandidate>> RunAlgorithmAsync(
        string algorithm, int? accountId, int? productId, int maxItems, CancellationToken ct)
    {
        return algorithm.ToLowerInvariant() switch
        {
            "trending" or "popular" => _trending.GetCandidatesAsync("global", maxItems, ct),
            "content_based" => productId.HasValue
                ? _contentBased.GetCandidatesAsync(productId.Value, maxItems, ct)
                : Task.FromResult(new List<RecommendationCandidate>()),
            "collaborative" => productId.HasValue
                ? _coPurchase.GetCandidatesAsync(productId.Value, maxItems, ct)
                : Task.FromResult(new List<RecommendationCandidate>()),
            "weighted_score" => accountId.HasValue
                ? _weighted.GetCandidatesAsync(accountId.Value, maxItems, ct)
                : Task.FromResult(new List<RecommendationCandidate>()),
            _ => Task.FromResult(new List<RecommendationCandidate>()),
        };
    }

    /// <summary>Fallback algorithm — nếu rỗng, dùng fallback config trong Widget.</summary>
    private async Task<List<RecommendationCandidate>> RunFallbackAsync(
        string fallbackAlgo, int? accountId, int? productId, int maxItems, CancellationToken ct)
    {
        var key = fallbackAlgo.ToLowerInvariant();

        // Special: pdp_similar fallback = trending của cùng category
        if (key == "popular" && productId.HasValue)
        {
            var prod = await _db.Products
                .AsNoTracking()
                .Where(p => p.ProductId == productId.Value)
                .Select(p => new { p.CategoryId })
                .FirstOrDefaultAsync(ct);
            if (prod != null)
                return await _trending.GetCategoryOrGlobalAsync(prod.CategoryId, maxItems, ct);
        }

        // Default fallback = Trending global
        return await _trending.GetCandidatesAsync("global", maxItems, ct);
    }

    private static string BuildCacheKey(string widgetCode, int? accountId, int? productId)
    {
        return $"{widgetCode}|acc:{accountId?.ToString() ?? "guest"}|prod:{productId?.ToString() ?? "-"}";
    }

    private async Task<RecommendationCacheDocument?> GetCachedAsync(string cacheKey, CancellationToken ct)
    {
        try
        {
            var doc = await _mongo.RecommendationCache
                .Find(x => x.CacheKey == cacheKey)
                .FirstOrDefaultAsync(ct);

            // Bảo vệ extra: nếu TTL chưa kịp xoá, kiểm tra ExpiresAt manually
            if (doc != null && doc.ExpiresAt > DateTime.UtcNow)
                return doc;

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read recommendation cache for {CacheKey}", cacheKey);
            return null;
        }
    }

    private async Task SaveCacheAsync(
        string cacheKey,
        string widgetCode,
        int? accountId,
        int? productId,
        List<RecommendationItemDto> items,
        CancellationToken ct)
    {
        try
        {
            var doc = new RecommendationCacheDocument
            {
                CacheKey = cacheKey,
                WidgetCode = widgetCode,
                AccountId = accountId,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(Math.Max(1, _options.CacheTtlMinutes)),
                Items = items.Select(i => new CachedRecommendationItem
                {
                    ProductId = i.ProductId,
                    Score = i.Score,
                    Reason = i.Reason,
                    ReasonCode = i.ReasonCode,
                }).ToList(),
            };

            var filter = Builders<RecommendationCacheDocument>.Filter.Eq(x => x.CacheKey, cacheKey);
            await _mongo.RecommendationCache.ReplaceOneAsync(
                filter, doc, new ReplaceOptions { IsUpsert = true }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save recommendation cache for {CacheKey}", cacheKey);
        }
    }

    /// <summary>
    /// Khi cache hit chỉ có ProductId + score, vẫn cần enrich data product để FE render.
    /// Cache cố tình KHÔNG lưu tên/giá/ảnh để tránh stale — luôn enrich realtime từ DB.
    /// </summary>
    private Task<List<RecommendationItemDto>> MapCachedItemsAsync(
        RecommendationCacheDocument cache, bool skipPurchasedFilter, CancellationToken ct)
    {
        var candidates = cache.Items.Select(i => new RecommendationCandidate
        {
            ProductId = i.ProductId,
            Score = i.Score,
            Reason = i.Reason,
            ReasonCode = i.ReasonCode,
        }).ToList();

        // Enrich + filter business rules (sp ngừng bán/hết hàng sẽ bị loại khỏi cache realtime)
        return _filter.FilterAndEnrichAsync(candidates, candidates.Count, userProfile: null, ct, skipPurchasedFilter);
    }
}
