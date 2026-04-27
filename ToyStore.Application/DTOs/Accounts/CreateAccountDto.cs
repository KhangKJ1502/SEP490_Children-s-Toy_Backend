namespace ToyStore.Application.DTOs.Accounts;

public class CreateAccountDto
{
    public byte RoleId { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
