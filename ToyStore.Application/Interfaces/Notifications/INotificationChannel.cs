using ToyStore.Application.DTOs.Notifications;

namespace ToyStore.Application.Interfaces.Notifications;

/// <summary>
/// Kênh gửi thông báo (WEB_BELL hoặc EMAIL).
/// Mỗi implementation chịu trách nhiệm INSERT Delivery và/hoặc gửi thực tế.
/// </summary>
public interface INotificationChannel
{
    string Channel { get; }

    Task SendAsync(NotificationDeliveryRequest request, CancellationToken ct = default);
}
