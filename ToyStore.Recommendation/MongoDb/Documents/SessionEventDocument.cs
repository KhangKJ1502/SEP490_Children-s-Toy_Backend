using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ToyStore.Recommendation.MongoDb.Documents;

/// <summary>
/// Document tạm lưu event do FE gửi lên (buffer trước khi flush sang SQL Server.Interaction.Events).
/// </summary>
public class SessionEventDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>AccountID — nullable nếu là khách vãng lai.</summary>
    [BsonElement("accountId")]
    public int? AccountId { get; set; }

    /// <summary>SessionID lấy từ trình duyệt (cookie/local storage).</summary>
    [BsonElement("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Loại event: product_view, add_to_cart, purchase, ...</summary>
    [BsonElement("eventType")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>EntityId — ID của object liên quan (ProductId/CategoryId/...). Lưu string để linh hoạt.</summary>
    [BsonElement("entityId")]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>EntityType: product/category/search ...</summary>
    [BsonElement("entityType")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Source page (home/pdp/cart/...).</summary>
    [BsonElement("source")]
    public string? Source { get; set; }

    /// <summary>Referrer URL.</summary>
    [BsonElement("referrer")]
    public string? Referrer { get; set; }

    /// <summary>Thiết bị: desktop/mobile/tablet.</summary>
    [BsonElement("deviceType")]
    public string? DeviceType { get; set; }

    /// <summary>Thời gian xem (ms) — dùng cho product_view_long.</summary>
    [BsonElement("durationMs")]
    public int? DurationMs { get; set; }

    /// <summary>Độ sâu cuộn trang (0-100).</summary>
    [BsonElement("scrollDepth")]
    public byte? ScrollDepth { get; set; }

    /// <summary>Vị trí click (toạ độ hoặc selector).</summary>
    [BsonElement("clickPosition")]
    public string? ClickPosition { get; set; }

    /// <summary>Metadata JSON tự do (lưu string đã JSON.stringify từ FE).</summary>
    [BsonElement("metadata")]
    public string? Metadata { get; set; }

    /// <summary>Thời điểm event xảy ra phía client (UTC).</summary>
    [BsonElement("occurredAt")]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm document được tạo ở server (UTC).</summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm event đã flush sang SQL Server. Null = chưa flush.</summary>
    [BsonElement("flushedAt")]
    public DateTime? FlushedAt { get; set; }
}
