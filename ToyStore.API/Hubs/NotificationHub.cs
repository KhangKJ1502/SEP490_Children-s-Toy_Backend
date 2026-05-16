using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Interfaces.Notifications;

namespace ToyStore.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly INotificationReadService _readService;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(INotificationReadService readService, ILogger<NotificationHub> logger)
    {
        _readService = readService;
        _logger      = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var accountId = Context.UserIdentifier;
        if (accountId is null)
        {
            _logger.LogWarning("NotificationHub: connected with no UserIdentifier");
            await base.OnConnectedAsync();
            return;
        }

        // User-specific group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{accountId}");

        // Role-based group (broadcast to all staff/admin/merch)
        var roleName = Context.User?.FindFirst("RoleName")?.Value;
        if (!string.IsNullOrEmpty(roleName))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{roleName}");

        _logger.LogDebug("NotificationHub: connected AccountID={AccountId}", accountId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var accountId = Context.UserIdentifier;
        if (accountId is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{accountId}");

            var roleName = Context.User?.FindFirst("RoleName")?.Value;
            if (!string.IsNullOrEmpty(roleName))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role_{roleName}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client gọi để mark 1 notification là đã đọc; hub sync tất cả tab của cùng user.
    /// </summary>
    public async Task MarkAsRead(long deliveryId)
    {
        var accountId = Context.UserIdentifier;
        if (accountId is null) return;

        if (!int.TryParse(accountId, out var accountIdInt)) return;

        try
        {
            await _readService.MarkReadAsync(deliveryId, accountIdInt);

            var unreadCount = await _readService.GetUnreadCountAsync(accountIdInt);

            await Clients.Group($"user_{accountId}")
                         .SendAsync("NotificationRead", deliveryId);

            await Clients.Group($"user_{accountId}")
                         .SendAsync("UnreadCountUpdated", unreadCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MarkAsRead for DeliveryId {DeliveryId}, AccountId {AccountId}", deliveryId, accountIdInt);
        }
    }
}
