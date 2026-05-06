namespace ToyStore.Application.DTOs.Profiles;

public class ChangeCustomerPasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;

    public string ConfirmNewPassword { get; set; } = string.Empty;
}
