using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace ToyStore.Infrastructure.Hubs;

public sealed class AccountIdUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var user = connection.User;
        if (user == null)
        {
            return null;
        }

        return user.Claims.FirstOrDefault(c => c.Type == "accountId")?.Value
               ?? user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
               ?? user.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
    }
}
