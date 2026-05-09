using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace ToyStore.API.Hubs;

/// <summary>
/// Resolves the SignalR user identifier from JWT claims (AccountID, NameIdentifier, or Sub).
/// This ensures consistent user tracking across different hub types.
/// </summary>
public sealed class AccountIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var user = connection.User;
        if (user == null) return null;

        return user.Claims.FirstOrDefault(c => c.Type == "AccountID" || c.Type == "accountId")?.Value
               ?? user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
               ?? user.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
    }
}
