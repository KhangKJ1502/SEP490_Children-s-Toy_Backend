namespace ToyStore.Application.DTOs.Accounts;

public class UpdateAccountInfoDto
{
    public string AccountName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public bool? IsActive { get; set; }
}
