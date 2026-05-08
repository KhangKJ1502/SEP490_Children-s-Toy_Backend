namespace ToyStore.Application.DTOs.Profiles;

public class CustomerProfileDto
{
    public int AccountId { get; set; }

    public byte RoleId { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = string.Empty;

    public DateTime? Dob { get; set; }

    public byte? SexId { get; set; }

    public string? SexName { get; set; }

    public string? ImageUrl { get; set; }

    public string? Provider { get; set; }
}
