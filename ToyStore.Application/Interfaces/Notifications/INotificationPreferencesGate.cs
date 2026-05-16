namespace ToyStore.Application.Interfaces.Notifications;

/// <summary>
/// Kiểm tra UserPreferences trước khi gửi thông báo theo kênh.
/// Phân biệt rõ kênh WEB_BELL (không cần EmailOptIn) và EMAIL (cần EmailOptIn).
/// </summary>
public interface INotificationPreferencesGate
{
    /// <summary>
    /// Trả về true nếu được phép gửi theo kênh và loại thông báo này.
    /// </summary>
    /// <param name="accountId">Người nhận</param>
    /// <param name="channel">NotificationChannels.WebBell | Email</param>
    /// <param name="notificationType">NotificationTypes.Order | Promotion | Stock | Blog | System</param>
    Task<bool> CanSendAsync(
        int accountId,
        string channel,
        string notificationType,
        CancellationToken ct = default);
}
