using Microsoft.EntityFrameworkCore;
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
}
