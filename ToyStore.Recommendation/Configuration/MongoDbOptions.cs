namespace ToyStore.Recommendation.Configuration;

/// <summary>
/// Cấu hình kết nối MongoDB cho hệ thống Recommendation.
/// Đọc từ section "MongoDb" trong appsettings.
/// </summary>
public class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    /// <summary>Connection string MongoDB. Ví dụ: mongodb://localhost:27017</summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>Tên database trong MongoDB.</summary>
    public string DatabaseName { get; set; } = "toystore_recommendation";

    /// <summary>Tên collection lưu user preference profile.</summary>
    public string UserProfilesCollection { get; set; } = "user_profiles";

    /// <summary>Tên collection cache recommendation kết quả.</summary>
    public string RecommendationCacheCollection { get; set; } = "recommendation_cache";
}
