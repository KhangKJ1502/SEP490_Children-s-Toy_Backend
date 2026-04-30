using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int AccountId
    {
        get
        {
            var value = User?.Claims.FirstOrDefault(c => c.Type == "accountId")?.Value;
            return int.TryParse(value, out var id) ? id : 0;
        }
    }

    public string Email => User?.Claims.FirstOrDefault(c =>
        c.Type == ClaimTypes.Email || c.Type == JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;

    public string RoleName => User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? string.Empty;

    public string? Jti => User?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

    public DateTime? TokenExpiry
    {
        get
        {
            var value = User?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
            if (long.TryParse(value, out var exp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
            }
            return null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
