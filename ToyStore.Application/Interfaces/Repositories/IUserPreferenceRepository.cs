using ToyStore.Application.DTOs.Profiles;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IUserPreferenceRepository
{
    Task<UserPreference?> GetByAccountIdAsync(int accountId, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates preferences for an account (tracked write).
    /// </summary>
    Task<UserPreference> UpsertForAccountAsync(
        int accountId,
        UpdateCustomerNotificationPreferencesDto dto,
        CancellationToken ct = default);
}
