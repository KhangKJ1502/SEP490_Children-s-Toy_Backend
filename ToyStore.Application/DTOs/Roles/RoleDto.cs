namespace ToyStore.Application.DTOs.Roles;

public class RoleDto
{
    public byte RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
