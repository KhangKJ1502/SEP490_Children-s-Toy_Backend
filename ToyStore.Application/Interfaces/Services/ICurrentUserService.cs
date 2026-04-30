namespace ToyStore.Application.Interfaces.Services;

public interface ICurrentUserService
{
    int AccountId { get; }
    string Email { get; }
    string RoleName { get; }
    string? Jti { get; }
    DateTime? TokenExpiry { get; }
    bool IsAuthenticated { get; }
}
