using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace ToyStore.API.Hubs;

/// <summary>
/// Trích xuất và giải quyết định danh người dùng (AccountId) từ các Claims trong JWT Token để gán ID người dùng cho kết nối SignalR.
/// </summary>
public sealed class AccountIdProvider : IUserIdProvider
{
    /// <summary>
    /// Lấy ID người dùng từ thông tin xác thực của kết nối SignalR dựa trên các Claim (AccountID, NameIdentifier, hoặc Sub).
    /// </summary>
    public string? GetUserId(HubConnectionContext connection)
    {
        var user = connection.User;
        if (user == null) return null;

        return user.Claims.FirstOrDefault(c => c.Type == "AccountID" || c.Type == "accountId")?.Value
               ?? user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
               ?? user.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
    }
}
