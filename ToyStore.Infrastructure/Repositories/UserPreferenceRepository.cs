using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Profiles;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class UserPreferenceRepository : IUserPreferenceRepository
{
    private readonly SEP490ToyStoreContext _db;

    public UserPreferenceRepository(SEP490ToyStoreContext db)
    {
        _db = db;
    }

    public async Task<UserPreference?> GetByAccountIdAsync(int accountId, CancellationToken ct = default)
    {
        return await _db.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.AccountId == accountId, ct);
    }

    public async Task<UserPreference> UpsertForAccountAsync(
        int accountId,
        UpdateCustomerNotificationPreferencesDto dto,
        CancellationToken ct = default)
    {
        var entity = await _db.UserPreferences
            .FirstOrDefaultAsync(p => p.AccountId == accountId, ct);

        if (entity is null)
        {
            entity = new UserPreference
            {
                AccountId = accountId,
            };
            _db.UserPreferences.Add(entity);
        }

        entity.EmailOptIn = dto.EmailOptIn;
        entity.WebPushOptIn = dto.WebPushOptIn;
        entity.OrderUpdates = dto.OrderUpdates;
        entity.Promotions = dto.Promotions;
        entity.StockAlerts = dto.StockAlerts;
        entity.BlogAlerts = dto.BlogAlerts;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return entity;
    }
}
