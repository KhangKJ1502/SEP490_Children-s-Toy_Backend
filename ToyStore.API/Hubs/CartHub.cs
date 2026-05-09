using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ToyStore.API.Hubs;

[Authorize]
public class CartHub : Hub
{
    private readonly ILogger<CartHub> _logger;

    public CartHub(ILogger<CartHub> logger)
    {
        _logger = logger;
    }

    public static string BuildUserGroup(int accountId) => $"cart-user-{accountId}";

    public override async Task OnConnectedAsync()
    {
        var accountIdClaim = Context.UserIdentifier;

        if (int.TryParse(accountIdClaim, out var accountId) && accountId > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, BuildUserGroup(accountId));
            _logger.LogInformation("CartHub connected: ConnectionId={ConnectionId}, AccountId={AccountId}", Context.ConnectionId, accountId);
        }
        else
        {
            _logger.LogWarning("CartHub connected without valid AccountID. ConnectionId={ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }
}
