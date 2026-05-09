using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;

namespace ToyStore.Infrastructure.Notifications;

/// <summary>
/// Fallback hub service registered in projects that don't host SignalR (e.g. Worker).
/// No-op — push is skipped; clients recover via REST poll on reconnect.
/// </summary>
public class NoOpNotificationHubService : INotificationHubService
{
    public Task PushToUserAsync(int accountId, BellNotificationDto payload, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task PushToRoleGroupAsync(string roleName, BellNotificationDto payload, CancellationToken ct = default)
        => Task.CompletedTask;
}
