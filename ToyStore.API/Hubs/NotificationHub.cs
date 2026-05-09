using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ToyStore.Application.Constants;
using ToyStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ToyStore.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly SEP490ToyStoreContext _db;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(SEP490ToyStoreContext db, ILogger<NotificationHub> logger)
    {
        _db     = db;
        _logger = logger;
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

        // Role-based group (for broadcast to all staff/admin/merch)
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
    /// Client calls this to mark a delivery as read; hub syncs all other tabs of the same user.
    /// </summary>
    public async Task MarkAsRead(long deliveryId)
    {
        var accountId = Context.UserIdentifier;
        if (accountId is null) return;

        if (!int.TryParse(accountId, out var accountIdInt)) return;

        var delivery = await _db.Deliveries
            .FirstOrDefaultAsync(d => d.DeliveryId == deliveryId && d.AccountId == accountIdInt);

        if (delivery is not null)
        {
            delivery.Status    = NotificationStatuses.Read;
            delivery.ReadAt    = DateTime.UtcNow;
            delivery.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        var unreadCount = await _db.Deliveries
            .CountAsync(d => d.AccountId == accountIdInt
                          && d.Channel   == NotificationChannels.WebBell
                          && d.Status    == NotificationStatuses.Unread);

        // Notify all other tabs of the same user
        await Clients.Group($"user_{accountId}")
                     .SendAsync("NotificationRead", deliveryId);

        await Clients.Group($"user_{accountId}")
                     .SendAsync("UnreadCountUpdated", unreadCount);
    }
}
