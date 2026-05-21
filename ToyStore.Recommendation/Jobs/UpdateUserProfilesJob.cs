using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ToyStore.Infrastructure.Data;
using ToyStore.Recommendation.Configuration;
using ToyStore.Recommendation.MongoDb;
using ToyStore.Recommendation.MongoDb.Documents;

namespace ToyStore.Recommendation.Jobs;

/// <summary>
/// Job D — Update MongoDB.user_profiles.
/// Mỗi 1 giờ:
///  - Đọc UserProductScores + thông tin Product (Category/Brand/Age/Price).
///  - Tính top category, top brand, top age, priceRange (min/max/avg).
///  - Lấy 50 sản phẩm xem gần nhất (theo Events product_view).
///  - Lấy danh sách product đã mua (từ Orders đã Completed) — dùng để loại khỏi gợi ý.
/// </summary>
public class UpdateUserProfilesJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly RecommendationOptions _options;
    private readonly ILogger<UpdateUserProfilesJob> _logger;

    public UpdateUserProfilesJob(
        IServiceProvider services,
        IOptions<RecommendationOptions> options,
        ILogger<UpdateUserProfilesJob> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "UpdateUserProfilesJob starting — interval {Minutes} minutes",
            _options.UpdateUserProfilesIntervalMinutes);

        try { await Task.Delay(TimeSpan.FromMinutes(4), stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "UpdateUserProfilesJob iteration failed"); }

            try { await Task.Delay(TimeSpan.FromMinutes(_options.UpdateUserProfilesIntervalMinutes), stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var mongo = scope.ServiceProvider.GetRequiredService<MongoDbContext>();

        // 1. Lấy tất cả AccountId có score (lấy 1 chiều, bounded để không OOM)
        var accountIds = await db.UserProductScores
            .AsNoTracking()
            .Select(x => x.AccountId)
            .Distinct()
            .Take(50_000) // chống unbounded
            .ToListAsync(ct);

        if (accountIds.Count == 0)
        {
            _logger.LogInformation("UpdateUserProfilesJob: no user product scores");
            return;
        }

        // 2. Lấy đầy đủ score + product info cần thiết
        var scoresWithProduct = await db.UserProductScores
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.AccountId))
            .Join(db.Products.AsNoTracking().Where(p => !p.IsDeleted),
                  s => s.ProductId,
                  p => p.ProductId,
                  (s, p) => new
                  {
                      s.AccountId,
                      s.ProductId,
                      s.Score,
                      s.LastInteractedAt,
                      p.CategoryId,
                      p.BrandId,
                      p.Price,
                      AgeId = p.ProductDetail != null ? p.ProductDetail.AgeId : null,
                  })
            .ToListAsync(ct);

        // 3. Lấy purchased product cho từng account (CHỈ orders Completed/Delivered - tức đã hoàn tất thật sự)
        //    Không tính orders Pending/Processing/Shipped/... vì user có thể hủy
        var purchasedByAccount = await db.OrderDetails
            .AsNoTracking()
            .Where(od => !od.Order.IsDeleted
                      && od.Order.CancelledAt == null
                      && (od.Order.Status.StatusName == "Completed" || od.Order.Status.StatusName == "Delivered")
                      && accountIds.Contains(od.Order.AccountId))
            .Select(od => new { od.Order.AccountId, od.ProductId })
            .Distinct()
            .ToListAsync(ct);

        var purchasedMap = purchasedByAccount
            .GroupBy(x => x.AccountId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ProductId).Distinct().ToList());

        // 4. Lấy 50 product xem gần nhất cho mỗi account (lọc product_view trong 90 ngày để giảm tải)
        var viewCutoff = DateTime.UtcNow.AddDays(-90);
        var recentViews = await db.Events
            .AsNoTracking()
            .Where(e => e.AccountId != null
                     && accountIds.Contains(e.AccountId.Value)
                     && e.EntityType == "product"
                     && (e.EventType == "product_view" || e.EventType == "product_view_long")
                     && e.CreatedAt >= viewCutoff)
            .Select(e => new { e.AccountId, e.EntityId, e.CreatedAt })
            .ToListAsync(ct);

        var recentlyViewedMap = recentViews
            .GroupBy(x => x.AccountId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.CreatedAt)
                      .Select(x => int.TryParse(x.EntityId, out var id) ? id : 0)
                      .Where(id => id > 0)
                      .Distinct()
                      .Take(50)
                      .ToList());

        // 5. Build profile cho mỗi account & upsert MongoDB
        var bulk = new List<WriteModel<UserProfileDocument>>(accountIds.Count);
        var now = DateTime.UtcNow;
        var scoresByAccount = scoresWithProduct.GroupBy(x => x.AccountId);

        foreach (var accGroup in scoresByAccount)
        {
            var accountId = accGroup.Key;
            var items = accGroup.ToList();

            // Top 5 category theo tổng score
            var preferredCategories = items
                .GroupBy(x => (int)x.CategoryId)
                .Select(g => new PreferenceItem { Id = g.Key, Score = g.Sum(x => x.Score) })
                .OrderByDescending(x => x.Score)
                .Take(5)
                .ToList();

            // Top 5 brand theo tổng score (chỉ tính brand != null)
            var preferredBrands = items
                .Where(x => x.BrandId.HasValue)
                .GroupBy(x => (int)x.BrandId!.Value)
                .Select(g => new PreferenceItem { Id = g.Key, Score = g.Sum(x => x.Score) })
                .OrderByDescending(x => x.Score)
                .Take(5)
                .ToList();

            // Top 3 age group
            var preferredAges = items
                .Where(x => x.AgeId.HasValue)
                .GroupBy(x => (int)x.AgeId!.Value)
                .Select(g => new PreferenceItem { Id = g.Key, Score = g.Sum(x => x.Score) })
                .OrderByDescending(x => x.Score)
                .Take(3)
                .ToList();

            // Price range min/max/avg theo giá sản phẩm đã tương tác
            var priceRange = new PriceRangeProfile();
            if (items.Count > 0)
            {
                priceRange.Min = items.Min(x => x.Price);
                priceRange.Max = items.Max(x => x.Price);
                priceRange.Avg = Math.Round(items.Average(x => x.Price), 0, MidpointRounding.AwayFromZero);
            }

            var doc = new UserProfileDocument
            {
                AccountId = accountId,
                PreferredCategories = preferredCategories,
                PreferredBrands = preferredBrands,
                PreferredAges = preferredAges,
                PriceRange = priceRange,
                RecentlyViewed = recentlyViewedMap.TryGetValue(accountId, out var rv) ? rv : new List<int>(),
                PurchasedProductIds = purchasedMap.TryGetValue(accountId, out var pp) ? pp : new List<int>(),
                UpdatedAt = now,
            };

            var filter = Builders<UserProfileDocument>.Filter.Eq(x => x.AccountId, accountId);
            bulk.Add(new ReplaceOneModel<UserProfileDocument>(filter, doc) { IsUpsert = true });
        }

        if (bulk.Count == 0)
        {
            _logger.LogInformation("UpdateUserProfilesJob: no profiles to upsert");
            return;
        }

        await mongo.UserProfiles.BulkWriteAsync(bulk, new BulkWriteOptions { IsOrdered = false }, ct);

        _logger.LogInformation("UpdateUserProfilesJob done — {Count} user profiles upserted", bulk.Count);
    }
}
