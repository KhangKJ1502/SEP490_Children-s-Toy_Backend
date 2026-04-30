namespace ToyStore.Application.DTOs.Profiles;

public class UpdateProfileDto
{
    public string? ImageUrl { get; set; }

    public string? PhoneNumber { get; set; }

    public string? CurrentPassword { get; set; }

    public string? NewPassword { get; set; }

    public string? ConfirmNewPassword { get; set; }
}
