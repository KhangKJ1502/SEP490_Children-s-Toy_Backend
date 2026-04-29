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
