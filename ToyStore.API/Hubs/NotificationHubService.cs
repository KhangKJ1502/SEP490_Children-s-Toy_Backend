using Microsoft.AspNetCore.SignalR;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;

namespace ToyStore.API.Hubs;

public class NotificationHubService : INotificationHubService
{
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationHubService(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public async Task PushToUserAsync(int accountId, BellNotificationDto payload, CancellationToken ct = default)
        => await _hub.Clients
                     .Group($"user_{accountId}")
                     .SendAsync("ReceiveNotification", payload, ct);

    public async Task PushToRoleGroupAsync(string roleName, BellNotificationDto payload, CancellationToken ct = default)
        => await _hub.Clients
                     .Group($"role_{roleName}")
                     .SendAsync("ReceiveNotification", payload, ct);
}
