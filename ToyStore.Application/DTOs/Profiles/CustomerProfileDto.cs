namespace ToyStore.Application.DTOs.Profiles;

public class CustomerProfileDto
{
    public string AccountName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string PasswordHash { get; set; } = string.Empty;
}
