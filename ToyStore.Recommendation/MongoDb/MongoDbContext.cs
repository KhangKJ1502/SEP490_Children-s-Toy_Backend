using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ToyStore.Recommendation.Configuration;
using ToyStore.Recommendation.MongoDb.Documents;

namespace ToyStore.Recommendation.MongoDb;

/// <summary>
/// Wrapper duy nhất quản lý connection MongoDB + expose các collection của hệ Recommendation.
/// Đăng ký Singleton để tận dụng connection pool của driver.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbOptions _options;
    private readonly ILogger<MongoDbContext> _logger;

    public MongoDbContext(IOptions<MongoDbOptions> options, ILogger<MongoDbContext> logger)
    {
        _options = options.Value;
        _logger = logger;

        var client = new MongoClient(_options.ConnectionString);
        _database = client.GetDatabase(_options.DatabaseName);

        // Bảo đảm index tồn tại để query nhanh + TTL hoạt động đúng
        EnsureIndexes();
    }

    public IMongoCollection<SessionEventDocument> SessionEvents =>
        _database.GetCollection<SessionEventDocument>(_options.SessionEventsCollection);

    public IMongoCollection<UserProfileDocument> UserProfiles =>
        _database.GetCollection<UserProfileDocument>(_options.UserProfilesCollection);

    public IMongoCollection<RecommendationCacheDocument> RecommendationCache =>
        _database.GetCollection<RecommendationCacheDocument>(_options.RecommendationCacheCollection);

    private void EnsureIndexes()
    {
        try
        {
            // ── session_events: idx tìm event chưa flush + idx phụ trợ
            var sessionEventIndexes = new[]
            {
                new CreateIndexModel<SessionEventDocument>(
                    Builders<SessionEventDocument>.IndexKeys
                        .Ascending(x => x.FlushedAt)
                        .Ascending(x => x.CreatedAt),
                    new CreateIndexOptions { Name = "ix_flushedAt_createdAt" }),
                new CreateIndexModel<SessionEventDocument>(
                    Builders<SessionEventDocument>.IndexKeys.Ascending(x => x.SessionId),
                    new CreateIndexOptions { Name = "ix_sessionId" }),
                // TTL: xoá session_event đã flush sau 7 ngày để tránh phình MongoDB
                new CreateIndexModel<SessionEventDocument>(
                    Builders<SessionEventDocument>.IndexKeys.Ascending(x => x.FlushedAt),
                    new CreateIndexOptions
                    {
                        Name = "ttl_flushedAt_7d",
                        ExpireAfter = TimeSpan.FromDays(7),
                        Sparse = true
                    }),
            };
            SessionEvents.Indexes.CreateMany(sessionEventIndexes);

            // ── recommendation_cache: unique cacheKey + TTL theo ExpiresAt
            var cacheIndexes = new[]
            {
                new CreateIndexModel<RecommendationCacheDocument>(
                    Builders<RecommendationCacheDocument>.IndexKeys.Ascending(x => x.CacheKey),
                    new CreateIndexOptions { Name = "ux_cacheKey", Unique = true }),
                new CreateIndexModel<RecommendationCacheDocument>(
                    Builders<RecommendationCacheDocument>.IndexKeys.Ascending(x => x.ExpiresAt),
                    new CreateIndexOptions
                    {
                        Name = "ttl_expiresAt",
                        ExpireAfter = TimeSpan.Zero // MongoDB auto-xoá khi ExpiresAt < now
                    }),
            };
            RecommendationCache.Indexes.CreateMany(cacheIndexes);
        }
        catch (Exception ex)
        {
            // Không throw — index không tạo được vẫn cho phép app chạy, chỉ log cảnh báo
            _logger.LogWarning(ex, "Failed to ensure MongoDB indexes (will retry on next startup)");
        }
    }
}
