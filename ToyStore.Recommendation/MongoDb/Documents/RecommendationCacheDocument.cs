using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ToyStore.Recommendation.MongoDb.Documents;

/// <summary>
/// Cache kết quả gợi ý theo (widgetCode + accountId + productId) — TTL 1 giờ.
/// </summary>
public class RecommendationCacheDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>Cache key duy nhất: widgetCode + accountId + productId.</summary>
    [BsonElement("cacheKey")]
    public string CacheKey { get; set; } = string.Empty;

    [BsonElement("widgetCode")]
    public string WidgetCode { get; set; } = string.Empty;

    [BsonElement("accountId")]
    public int? AccountId { get; set; }

    [BsonElement("productId")]
    public int? ProductId { get; set; }

    [BsonElement("orderId")]
    public int? OrderId { get; set; }

    /// <summary>Danh sách item kèm score + reason để FE render.</summary>
    [BsonElement("items")]
    public List<CachedRecommendationItem> Items { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm cache hết hạn (UTC) — TTL index sẽ xoá khi tới giờ.</summary>
    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);
}

public class CachedRecommendationItem
{
    [BsonElement("productId")]
    public int ProductId { get; set; }

    [BsonElement("score")]
    public decimal Score { get; set; }

    [BsonElement("reason")]
    public string Reason { get; set; } = string.Empty;

    [BsonElement("reasonCode")]
    public string ReasonCode { get; set; } = string.Empty;
}
