using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Roles;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service quan ly Role.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Lay danh sach Role.
    /// </summary>
    Task<Result<List<RoleDto>>> GetRolesAsync(CancellationToken cancellationToken = default);
}
