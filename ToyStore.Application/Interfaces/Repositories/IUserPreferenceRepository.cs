using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IUserPreferenceRepository
{
    Task<UserPreference?> GetByAccountIdAsync(int accountId, CancellationToken ct = default);
}
