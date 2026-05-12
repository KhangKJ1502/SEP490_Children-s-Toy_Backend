namespace ToyStore.Application.DTOs.Customers;

public class CustomerDetailDto
{
    public int AccountId { get; set; }

    public byte RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = string.Empty;

    public DateTime? Dob { get; set; }

    public byte? SexId { get; set; }

    public string? SexName { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
