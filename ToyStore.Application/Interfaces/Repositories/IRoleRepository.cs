using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository thao tac du lieu Role.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Lay danh sach Role.
    /// </summary>
    Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default);
}
