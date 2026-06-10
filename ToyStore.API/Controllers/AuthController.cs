using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Auth;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// APIs xác thực người dùng.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ICurrentUserService currentUserService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Đăng nhập tài khoản.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.LoginAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gửi OTP xác nhận đăng ký qua email.
    /// </summary>
    [HttpPost("send-register-otp")]
    public async Task<ActionResult> SendRegisterOtp(
        [FromBody] SendRegisterOtpDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.SendRegisterOtpAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("register/request-otp")]
    public async Task<ActionResult> RequestRegisterOtp(
        [FromBody] RequestRegisterOtpDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.RequestRegisterOtpAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("register/resend-otp")]
    public async Task<ActionResult> ResendRegisterOtp(
        [FromBody] SendRegisterOtpDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.ResendRegisterOtpAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("register/verify-otp")]
    public async Task<ActionResult<AuthResponseDto>> VerifyRegisterOtp(
        [FromBody] VerifyRegisterOtpDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.VerifyRegisterOtpAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Đăng ký tài khoản khách hàng mới (yêu cầu OTP hợp lệ).
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AccountInfoDto>> Register(
        [FromBody] RegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.RegisterAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gửi OTP đặt lại mật khẩu qua email.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword(
        [FromBody] ForgotPasswordDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.SendForgotPasswordOtpAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Đặt lại mật khẩu bằng OTP.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword(
        [FromBody] ResetPasswordDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.ResetPasswordAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Đăng xuất tài khoản (vô hiệu hóa token hiện tại).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var jti = _currentUserService.Jti ?? string.Empty;
        var expiry = _currentUserService.TokenExpiry;
        var remainingTime = expiry.HasValue
            ? expiry.Value - DateTime.UtcNow
            : TimeSpan.FromHours(24);

        if (remainingTime <= TimeSpan.Zero)
        {
            remainingTime = TimeSpan.FromMinutes(1);
        }

        var result = await _authService.LogoutAsync(jti, remainingTime, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Login hoặc tự động register bằng Google OAuth (Customer).
    /// Nếu account chưa tồn tại, tự động tạo mới với role Customer.
    /// Admin login: cần chỉ định RoleId trong body và account phải đã tồn tại.
    /// </summary>
    [HttpPost("google-login")]
    public async Task<ActionResult<AuthResponseDto>> GoogleLogin(
        [FromBody] GoogleLoginDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.GoogleLoginAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Register account mới bằng Google OAuth (chỉ Customer).
    /// Nếu account đã tồn tại, trả lỗi.
    /// </summary>
    [HttpPost("google-register")]
    public async Task<ActionResult<AuthResponseDto>> GoogleRegister(
        [FromBody] GoogleRegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.GoogleRegisterAsync(dto, cancellationToken);
        return result.ToActionResult();
    }
}
