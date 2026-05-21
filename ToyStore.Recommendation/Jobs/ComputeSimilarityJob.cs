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
/// Job B — Tính ItemSimilarities (Content-Based).
/// Feature vector mỗi product gồm: CategoryID, BrandID, PriceRangeID, AgeID, SexID, OriginID, MaterialID.
/// Dùng cosine similarity giữa các vector One-Hot (mỗi feature trùng được tính 1 đơn vị).
/// Lưu top K (10) similar products cho mỗi product.
/// </summary>
public class ComputeSimilarityJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly RecommendationOptions _options;
    private readonly ILogger<ComputeSimilarityJob> _logger;

    public ComputeSimilarityJob(
        IServiceProvider services,
        IOptions<RecommendationOptions> options,
        ILogger<ComputeSimilarityJob> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ComputeSimilarityJob starting — interval {Minutes} minutes, topK {TopK}",
            _options.ComputeSimilarityIntervalMinutes, _options.SimilarityTopK);

        // Lệch 2 phút sau ComputeScores
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "ComputeSimilarityJob iteration failed"); }

            try { await Task.Delay(TimeSpan.FromMinutes(_options.ComputeSimilarityIntervalMinutes), stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();

        // 1. Lấy tất cả product Active (không deleted) kèm feature từ ProductDetail
        var products = await db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.ProductStatus == "Active")
            .Select(p => new ProductFeature
            {
                ProductId = p.ProductId,
                CategoryId = p.CategoryId,
                BrandId = p.BrandId,
                PriceRangeId = p.PriceRangeId,
                AgeId = p.ProductDetail != null ? p.ProductDetail.AgeId : null,
                SexId = p.ProductDetail != null ? p.ProductDetail.SexId : null,
                OriginId = p.ProductDetail != null ? p.ProductDetail.OriginId : null,
                MaterialId = p.ProductDetail != null ? p.ProductDetail.MaterialId : null,
            })
            .ToListAsync(ct);

        if (products.Count <= 1)
        {
            _logger.LogInformation("ComputeSimilarityJob: only {Count} active product(s) — skipped", products.Count);
            return;
        }

        _logger.LogInformation("ComputeSimilarityJob: analyzing {Count} active products", products.Count);

        // 2. Gom theo Category để giảm số cặp phải so sánh (chỉ so trong cùng CategoryID
        //    HOẶC cùng AgeId — tránh O(N^2) cho hàng ngàn sản phẩm)
        var byCategory = products
            .GroupBy(p => p.CategoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var now = DateTime.UtcNow;
        var allSimilarities = new List<ItemSimilarity>(products.Count * _options.SimilarityTopK);
        var topK = Math.Max(1, _options.SimilarityTopK);

        foreach (var product in products)
        {
            // Candidate set: cùng category + cùng age group (mở rộng hợp lý)
            var candidates = byCategory.TryGetValue(product.CategoryId, out var sameCat)
                ? sameCat.Where(c => c.ProductId != product.ProductId).ToList()
                : new List<ProductFeature>();

            if (candidates.Count == 0) continue;

            // Tính cosine với từng candidate
            var scored = new List<(int ProductId, decimal Score)>(candidates.Count);
            foreach (var c in candidates)
            {
                var sim = CosineSimilarity(product, c);
                if (sim <= 0) continue;
                scored.Add((c.ProductId, sim));
            }

            if (scored.Count == 0) continue;

            // Lấy top K theo similarity giảm dần
            var top = scored
                .OrderByDescending(s => s.Score)
                .Take(topK)
                .ToList();

            foreach (var t in top)
            {
                allSimilarities.Add(new ItemSimilarity
                {
                    SourceProductId = product.ProductId,
                    SimilarProductId = t.ProductId,
                    SimilarityScore = Math.Round(t.Score, 4, MidpointRounding.AwayFromZero),
                    AlgorithmType = "content_based",
                    UpdatedAt = now,
                    CreatedAt = now,
                });
            }
        }

        if (allSimilarities.Count == 0)
        {
            _logger.LogInformation("ComputeSimilarityJob: no similarity pairs generated");
            return;
        }

        // 3. Xoá data cũ của content_based + insert mới (đơn giản & an toàn cho dữ liệu nhỏ)
        //    Lưu ý: với DB lớn nên dùng MERGE; ở đây giữ approach đơn giản, vẫn chấp nhận được do
        //    số similarity ≤ products × 10.
        await db.ItemSimilarities
            .Where(x => x.AlgorithmType == "content_based")
            .ExecuteDeleteAsync(ct);

        await db.ItemSimilarities.AddRangeAsync(allSimilarities, ct);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ComputeSimilarityJob done — {Count} similarity pairs saved",
            allSimilarities.Count);
    }

    /// <summary>
    /// Cosine similarity giữa 2 feature vector one-hot.
    /// Vector chiều dài = số feature (7) nhưng chỉ chứa 0/1 cho mỗi cặp.
    /// Cosine = (intersection) / sqrt(|A| × |B|) — đơn giản & ổn định.
    /// </summary>
    private static decimal CosineSimilarity(ProductFeature a, ProductFeature b)
    {
        if (a.ProductId == b.ProductId) return 0m;

        // Tính số feature có giá trị (norm)
        var normA = CountFeatures(a);
        var normB = CountFeatures(b);
        if (normA == 0 || normB == 0) return 0m;

        var intersect = 0;
        if (a.CategoryId == b.CategoryId) intersect++;
        if (a.BrandId.HasValue && a.BrandId == b.BrandId) intersect++;
        if (a.PriceRangeId.HasValue && a.PriceRangeId == b.PriceRangeId) intersect++;
        if (a.AgeId.HasValue && a.AgeId == b.AgeId) intersect++;
        if (a.SexId.HasValue && a.SexId == b.SexId) intersect++;
        if (a.OriginId.HasValue && a.OriginId == b.OriginId) intersect++;
        if (a.MaterialId.HasValue && a.MaterialId == b.MaterialId) intersect++;

        if (intersect == 0) return 0m;
        var cosine = intersect / Math.Sqrt(normA * normB);
        return (decimal)cosine;
    }

    private static int CountFeatures(ProductFeature f)
    {
        var c = 1; // CategoryId luôn có
        if (f.BrandId.HasValue) c++;
        if (f.PriceRangeId.HasValue) c++;
        if (f.AgeId.HasValue) c++;
        if (f.SexId.HasValue) c++;
        if (f.OriginId.HasValue) c++;
        if (f.MaterialId.HasValue) c++;
        return c;
    }

    private sealed class ProductFeature
    {
        public int ProductId { get; set; }
        public short CategoryId { get; set; }
        public short? BrandId { get; set; }
        public byte? PriceRangeId { get; set; }
        public byte? AgeId { get; set; }
        public byte? SexId { get; set; }
        public byte? OriginId { get; set; }
        public short? MaterialId { get; set; }
    }
}
