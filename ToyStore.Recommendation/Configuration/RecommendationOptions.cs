namespace ToyStore.Recommendation.Configuration;

/// <summary>
/// Cấu hình hệ thống Recommendation đọc từ section "Recommendation" trong appsettings.
/// </summary>
public class RecommendationOptions
{
    public const string SectionName = "Recommendation";

    /// <summary>Tần suất chạy Flush events (phút). Mặc định 5 phút.</summary>
    public int FlushEventsIntervalMinutes { get; set; } = 5;

    /// <summary>Tần suất tính UserProductScores (phút). Mặc định 60 phút.</summary>
    public int ComputeScoresIntervalMinutes { get; set; } = 60;

    /// <summary>Tần suất tính ItemSimilarities (phút). Mặc định 60 phút.</summary>
    public int ComputeSimilarityIntervalMinutes { get; set; } = 60;

    /// <summary>Tần suất tính TrendingProducts (phút). Mặc định 60 phút.</summary>
    public int ComputeTrendingIntervalMinutes { get; set; } = 60;

    /// <summary>Tần suất update MongoDB user_profiles (phút). Mặc định 60 phút.</summary>
    public int UpdateUserProfilesIntervalMinutes { get; set; } = 60;

    /// <summary>Số ngày dữ liệu Events được dùng để tính score. Mặc định 30 ngày.</summary>
    public int ScoreWindowDays { get; set; } = 30;

    /// <summary>Số top similar products lưu cho mỗi sản phẩm. Mặc định 10.</summary>
    public int SimilarityTopK { get; set; } = 10;

    /// <summary>Số top trending sản phẩm cho mỗi scope. Mặc định 20.</summary>
    public int TrendingTopK { get; set; } = 20;

    /// <summary>Cửa sổ trending (giờ). Mặc định 24h.</summary>
    public int TrendingWindowHours { get; set; } = 24;

    /// <summary>TTL cache recommendation (phút). Mặc định 60 phút.</summary>
    public int CacheTtlMinutes { get; set; } = 60;

    /// <summary>Tracking endpoint có hoạt động hay không (cho phép tắt nhanh khi cần).</summary>
    public bool TrackingEnabled { get; set; } = true;

    /// <summary>Số event tối đa cho phép trong 1 batch upload từ FE.</summary>
    public int MaxEventsPerBatch { get; set; } = 50;

    /// <summary>Batch size khi flush events vào SQL Server.</summary>
    public int FlushBatchSize { get; set; } = 500;
}
