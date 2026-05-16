namespace ToyStore.Application.DTOs.Notifications;

/// <summary>
/// Dữ liệu đầu vào cho NotificationDispatcher.
/// - TemplateCode: bắt buộc — phải khớp với bảng [Notification].[Templates].
/// - Placeholders: các giá trị để render template (vd: {"OrderCode": "ORD-001"}).
/// - ReferenceId: dùng để tạo IdempotencyKey tự động ({TemplateCode}:{AccountId}:{ReferenceId}:{Channel}).
///   Nếu set IdempotencyKey trực tiếp (campaign path), ReferenceId không cần thiết.
/// - Title / Message: nếu đã set (campaign tự render), dispatcher dùng luôn mà không lookup template.
/// </summary>
public record NotificationContext
{
    public required int RecipientAccountId { get; init; }
    public required string RecipientType { get; init; }
    public required string NotificationType { get; init; }
    // TemplateCode phải tồn tại trong bảng [Notification].[Templates] (FK).
    // Null chỉ chấp nhận cho campaign không có template (TitleOverride/MessageOverride).
    public string? TemplateCode { get; init; }

    // Dữ liệu để render template — khớp key với placeholder trong TitleTemplate/MessageTemplate
    public IReadOnlyDictionary<string, string>? Placeholders { get; init; }

    // Dùng để tạo IdempotencyKey = "{TemplateCode}:{AccountId}:{ReferenceId}:{Channel}"
    public string? ReferenceId { get; init; }

    // Pre-rendered (campaign path tự render rồi truyền vào; dispatcher dùng luôn nếu != null)
    public string? Title { get; init; }
    public string? Message { get; init; }

    public bool SendBell { get; init; } = true;
    public bool SendEmail { get; init; } = false;

    public string? ImageUrl { get; init; }
    public string? ActionType { get; init; }
    public string? ActionTarget { get; init; }
    public Dictionary<string, object>? Payload { get; init; }
    public int? CampaignId { get; init; }

    // Override IdempotencyKey tự động (dùng cho campaign và các trường hợp đặc biệt)
    public string? IdempotencyKey { get; init; }
}
