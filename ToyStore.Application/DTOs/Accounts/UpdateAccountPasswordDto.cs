namespace ToyStore.Application.DTOs.Accounts;

public class UpdateAccountPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;

    public string ConfirmNewPassword { get; set; } = string.Empty;
}
