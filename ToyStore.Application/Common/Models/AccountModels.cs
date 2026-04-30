namespace ToyStore.Application.Common.Models;

public class AccountModel
{
    public int AccountId { get; set; }

    public byte RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string? EmployeeCode { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public string? Provider { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class AccountRoleModel
{
    public byte RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}

public class AccountAuthModel
{
    public int AccountId { get; set; }

    public byte RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }
}
