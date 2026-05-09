using ToyStore.Application.DTOs.Notifications;

namespace ToyStore.Application.Interfaces.Notifications;

public interface INotificationDispatcher
{
    Task DispatchAsync(NotificationContext context, CancellationToken ct = default);

    Task DispatchBulkAsync(IEnumerable<NotificationContext> contexts, CancellationToken ct = default);
}
