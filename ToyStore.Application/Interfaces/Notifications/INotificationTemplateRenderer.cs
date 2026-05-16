namespace ToyStore.Application.Interfaces.Notifications;

/// <summary>
/// Đọc template từ [Notification].[Templates], thay thế placeholder và trả về
/// nội dung đã render. Cache template trong memory để tránh query DB lặp.
/// </summary>
public interface INotificationTemplateRenderer
{
    /// <summary>
    /// Render template theo TemplateCode.
    /// Throw <see cref="ToyStore.Application.Exceptions.TemplateNotFoundException"/>
    /// nếu template không tồn tại, IsActive = 0, hoặc IsDeleted = 1.
    /// </summary>
    Task<(string Title, string Message)> RenderAsync(
        string templateCode,
        IReadOnlyDictionary<string, string>? placeholders,
        CancellationToken ct = default);
}
