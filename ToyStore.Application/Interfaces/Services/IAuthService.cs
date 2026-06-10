using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs.Auth;

namespace ToyStore.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);

    Task<Result> SendRegisterOtpAsync(SendRegisterOtpDto dto, CancellationToken cancellationToken = default);

    Task<Result> RequestRegisterOtpAsync(RequestRegisterOtpDto dto, CancellationToken cancellationToken = default);

    Task<Result> ResendRegisterOtpAsync(SendRegisterOtpDto dto, CancellationToken cancellationToken = default);

    Task<Result<AuthResponseDto>> VerifyRegisterOtpAsync(VerifyRegisterOtpDto dto, CancellationToken cancellationToken = default);

    Task<Result<AccountInfoDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);

    Task<Result> SendForgotPasswordOtpAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default);

    Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(string jti, TimeSpan remainingTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Login hoặc tự động register account bằng Google OAuth (Customer).
    /// Nếu account chưa tồn tại, tự động tạo mới với role Customer và random password.
    /// Nếu account đã tồn tại, login và trả về JWT token.
    /// </summary>
    Task<Result<AuthResponseDto>> GoogleLoginAsync(GoogleLoginDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register account mới bằng Google OAuth (chỉ Customer).
    /// Nếu account đã tồn tại, trả lỗi.
    /// Nếu chưa tồn tại, tạo mới với role Customer và random password.
    /// </summary>
    Task<Result<AuthResponseDto>> GoogleRegisterAsync(GoogleRegisterDto dto, CancellationToken cancellationToken = default);
}
