namespace ToyStore.Application.Interfaces.Services;

public interface IRedisService
{
    Task SetAsync(string key, string value, TimeSpan expiry);

    Task<string?> GetAsync(string key);

    Task DeleteAsync(string key);

    Task<bool> ExistsAsync(string key);

    Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiry);
}
