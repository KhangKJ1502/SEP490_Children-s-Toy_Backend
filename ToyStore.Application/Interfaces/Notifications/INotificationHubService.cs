using ToyStore.Application.DTOs.Notifications;

namespace ToyStore.Application.Interfaces.Notifications;

public interface INotificationHubService
{
    Task PushToUserAsync(int accountId, BellNotificationDto payload, CancellationToken ct = default);

    Task PushToRoleGroupAsync(string roleName, BellNotificationDto payload, CancellationToken ct = default);
}
