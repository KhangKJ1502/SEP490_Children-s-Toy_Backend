namespace ToyStore.Application.DTOs.Accounts;

public class AccountListDto
{
    public int AccountId { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public byte RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
