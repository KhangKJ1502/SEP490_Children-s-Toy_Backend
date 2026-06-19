using StackExchange.Redis;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

public class RedisService : IRedisService
{
    private readonly IDatabase _database;

    public RedisService(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task SetAsync(string key, string value, TimeSpan expiry)
    {
        await _database.StringSetAsync(key, value, expiry);
    }

    public async Task<string?> GetAsync(string key)
    {
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task DeleteAsync(string key)
    {
        await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _database.KeyExistsAsync(key);
    }

    public async Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiry)
    {
        // Redis SET NX EX — atomic, trả về true nếu key chưa tồn tại và đã set thành công
        return await _database.StringSetAsync(key, value, expiry, When.NotExists);
    }
}
