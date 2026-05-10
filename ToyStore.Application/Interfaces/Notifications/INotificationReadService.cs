namespace ToyStore.Application.Interfaces.Notifications;

public interface INotificationReadService
{
    Task MarkReadAsync(long deliveryId, int accountId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(int accountId, CancellationToken ct = default);
}
