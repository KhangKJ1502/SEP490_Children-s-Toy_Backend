using MongoDB.Bson.Serialization.Attributes;

namespace ToyStore.Recommendation.MongoDb.Documents;

/// <summary>
/// Profile sở thích tổng hợp của 1 user — dùng cho Content-Based + filter business rules.
/// _id = AccountId để upsert nhanh.
/// </summary>
public class UserProfileDocument
{
    [BsonId]
    public int AccountId { get; set; }

    /// <summary>Top CategoryID người dùng tương tác nhiều nhất (sort theo score giảm dần).</summary>
    [BsonElement("preferredCategories")]
    public List<PreferenceItem> PreferredCategories { get; set; } = new();

    /// <summary>Top BrandID người dùng tương tác nhiều nhất.</summary>
    [BsonElement("preferredBrands")]
    public List<PreferenceItem> PreferredBrands { get; set; } = new();

    /// <summary>Top AgeID (group tuổi) phù hợp với hành vi user.</summary>
    [BsonElement("preferredAges")]
    public List<PreferenceItem> PreferredAges { get; set; } = new();

    /// <summary>Khoảng giá: min/max/avg của các sản phẩm đã tương tác (VND).</summary>
    [BsonElement("priceRange")]
    public PriceRangeProfile PriceRange { get; set; } = new();

    /// <summary>50 ProductID xem gần nhất (giảm dần theo thời gian).</summary>
    [BsonElement("recentlyViewed")]
    public List<int> RecentlyViewed { get; set; } = new();

    /// <summary>Danh sách ProductID đã mua — dùng để loại khỏi gợi ý.</summary>
    [BsonElement("purchasedProductIds")]
    public List<int> PurchasedProductIds { get; set; } = new();

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PreferenceItem
{
    [BsonElement("id")]
    public int Id { get; set; }

    [BsonElement("score")]
    public decimal Score { get; set; }
}

public class PriceRangeProfile
{
    [BsonElement("min")]
    public decimal Min { get; set; }

    [BsonElement("max")]
    public decimal Max { get; set; }

    [BsonElement("avg")]
    public decimal Avg { get; set; }
}
