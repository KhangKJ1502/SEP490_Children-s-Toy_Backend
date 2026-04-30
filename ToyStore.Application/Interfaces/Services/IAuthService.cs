using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs.Auth;

namespace ToyStore.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);

    Task<Result> SendRegisterOtpAsync(SendRegisterOtpDto dto, CancellationToken cancellationToken = default);

    Task<Result<AccountInfoDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);

    Task<Result> SendForgotPasswordOtpAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default);

    Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(string jti, TimeSpan remainingTime, CancellationToken cancellationToken = default);
}
