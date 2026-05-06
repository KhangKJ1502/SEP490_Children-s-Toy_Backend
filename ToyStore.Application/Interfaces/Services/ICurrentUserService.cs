namespace ToyStore.Application.Interfaces.Services;

public interface ICurrentUserService
{
    int AccountId { get; }
    byte RoleId { get; }
    string Email { get; }
    string RoleName { get; }
    string? Jti { get; }
    DateTime? TokenExpiry { get; }
    bool IsAuthenticated { get; }
}
