namespace ToyStore.Application.DTOs.Tracking;

/// <summary>
/// Body request gửi từ FE tới POST /api/tracking.
/// FE gom event mỗi 30s / 10 events / khi user rời trang rồi gửi 1 batch.
/// </summary>
public class TrackEventRequestDto
{
    /// <summary>SessionID FE tự sinh (lưu localStorage).</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>AccountID (nullable nếu khách vãng lai).</summary>
    public int? AccountId { get; set; }

    /// <summary>Danh sách event trong batch (≤ MaxEventsPerBatch).</summary>
    public List<TrackEventItemDto> Events { get; set; } = new();
}

/// <summary>
/// 1 event trong batch.
/// </summary>
public class TrackEventItemDto
{
    /// <summary>Loại event: product_view | product_view_long | add_to_cart | ...</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>EntityId: ID của object (ProductId/CategoryId/...) — string vì có thể là từ khoá tìm kiếm.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>EntityType: product | category | search | ...</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Source page (home/pdp/cart/list/...).</summary>
    public string? Source { get; set; }

    /// <summary>Referrer URL.</summary>
    public string? Referrer { get; set; }

    /// <summary>desktop | mobile | tablet</summary>
    public string? DeviceType { get; set; }

    /// <summary>Thời gian xem chi tiết (ms) cho product_view_long.</summary>
    public int? DurationMs { get; set; }

    /// <summary>Scroll depth (0..100).</summary>
    public byte? ScrollDepth { get; set; }

    /// <summary>Vị trí click (toạ độ "x,y" hoặc tên section).</summary>
    public string? ClickPosition { get; set; }

    /// <summary>Metadata JSON tự do — FE đã JSON.stringify.</summary>
    public string? Metadata { get; set; }

    /// <summary>Thời điểm event xảy ra ở client (ISO 8601 UTC). Nếu null, server sẽ set theo UTC now.</summary>
    public DateTime? OccurredAt { get; set; }
}
