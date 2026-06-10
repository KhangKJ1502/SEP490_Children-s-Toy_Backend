namespace ToyStore.Application.DTOs.Auth;

public class LoginDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class SendRegisterOtpDto
{
    public string Email { get; set; } = null!;
}

public class RegisterDto
{
    public string AccountName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
    public string OtpCode { get; set; } = null!;
}

public class RequestRegisterOtpDto
{
    public string AccountName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}

public class VerifyRegisterOtpDto
{
    public string Email { get; set; } = null!;
    public string OtpCode { get; set; } = null!;
}

public class ForgotPasswordDto
{
    public string Email { get; set; } = null!;
}

public class ResetPasswordDto
{
    public string Email { get; set; } = null!;
    public string OtpCode { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public AccountInfoDto Account { get; set; } = null!;
}

public class AccountInfoDto
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public byte RoleId { get; set; }
    public string RoleName { get; set; } = null!;
}

/// <summary>
/// DTO cho login bằng Google OAuth - Customer và Admin.
/// </summary>
public class GoogleLoginDto
{
    /// <summary>
    /// Google ID Token (JWT) nhận từ frontend sau khi user đăng nhập Google.
    /// </summary>
    public string IdToken { get; set; } = null!;
    
    /// <summary>
    /// RoleId mong muốn - chỉ dùng cho Admin login (tùy chọn).
    /// Nếu null: mặc định là Customer (RoleId = 1).
    /// Nếu có giá trị: kiểm tra xem account đã tồn tại có role này không.
    /// </summary>
    public byte? RoleId { get; set; }
}

/// <summary>
/// DTO cho register bằng Google OAuth - chỉ Customer.
/// Admin không được phép register bằng Google, chỉ login nếu account đã tồn tại.
/// </summary>
public class GoogleRegisterDto
{
    /// <summary>
    /// Google ID Token (JWT) nhận từ frontend sau khi user đăng nhập Google.
    /// </summary>
    public string IdToken { get; set; } = null!;
}
