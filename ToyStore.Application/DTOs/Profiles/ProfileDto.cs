namespace ToyStore.Application.DTOs.Profiles;

public class ProfileDto
{
    public int AccountId { get; set; }

    public byte RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string? EmployeeCode { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
