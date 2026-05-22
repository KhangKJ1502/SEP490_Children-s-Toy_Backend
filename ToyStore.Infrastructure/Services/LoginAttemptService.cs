using Microsoft.Extensions.Caching.Memory;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Service quản lý các lần thử đăng nhập thất bại
/// Sử dụng MemoryCache để lưu trữ tạm thời
/// </summary>
public class LoginAttemptService : ILoginAttemptService
{
    private readonly IMemoryCache _cache;
    private const int MaxFailedAttempts = 5; // Số lần thất bại tối đa
    private const int LockoutMinutes = 5; // Thời gian khóa (phút)
    private const int TrackingWindowMinutes = 30; // Cửa sổ thời gian theo dõi (phút)

    public LoginAttemptService(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Lấy cache key cho failed attempts
    /// </summary>
    private string GetAttemptsKey(string email) => $"login_attempts:{email.ToLowerInvariant()}";

    /// <summary>
    /// Lấy cache key cho lockout time
    /// </summary>
    private string GetLockoutKey(string email) => $"login_lockout:{email.ToLowerInvariant()}";

    public int RecordFailedAttempt(string email)
    {
        var key = GetAttemptsKey(email);
        var lockoutKey = GetLockoutKey(email);

        // Lấy số lần thất bại hiện tại
        var attempts = _cache.Get<List<DateTime>>(key) ?? new List<DateTime>();

        // Thêm thời điểm thất bại mới
        attempts.Add(DateTime.UtcNow);

        // Lọc các attempts trong 30 phút gần nhất
        var recentAttempts = attempts
            .Where(a => a > DateTime.UtcNow.AddMinutes(-TrackingWindowMinutes))
            .ToList();

        // Lưu lại vào cache với thời gian hết hạn là 30 phút
        _cache.Set(key, recentAttempts, TimeSpan.FromMinutes(TrackingWindowMinutes));

        // Nếu đạt số lần tối đa, khóa tài khoản
        if (recentAttempts.Count >= MaxFailedAttempts)
        {
            var lockoutUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
            _cache.Set(lockoutKey, lockoutUntil, TimeSpan.FromMinutes(LockoutMinutes + 1));
            return 0;
        }

        // Trả về số lần còn lại
        return MaxFailedAttempts - recentAttempts.Count;
    }

    public void ResetAttempts(string email)
    {
        var key = GetAttemptsKey(email);
        var lockoutKey = GetLockoutKey(email);
        
        _cache.Remove(key);
        _cache.Remove(lockoutKey);
    }

    public bool IsLocked(string email)
    {
        var lockoutKey = GetLockoutKey(email);
        var lockoutUntil = _cache.Get<DateTime?>(lockoutKey);

        if (lockoutUntil == null)
            return false;

        // Kiểm tra xem thời gian khóa đã hết chưa
        if (lockoutUntil.Value <= DateTime.UtcNow)
        {
            _cache.Remove(lockoutKey);
            return false;
        }

        return true;
    }

    public int GetRemainingLockTimeInSeconds(string email)
    {
        var lockoutKey = GetLockoutKey(email);
        var lockoutUntil = _cache.Get<DateTime?>(lockoutKey);

        if (lockoutUntil == null || lockoutUntil.Value <= DateTime.UtcNow)
            return 0;

        return (int)(lockoutUntil.Value - DateTime.UtcNow).TotalSeconds;
    }

    public int GetFailedAttempts(string email)
    {
        var key = GetAttemptsKey(email);
        var attempts = _cache.Get<List<DateTime>>(key) ?? new List<DateTime>();

        // Lọc các attempts trong 30 phút gần nhất
        var recentAttempts = attempts
            .Where(a => a > DateTime.UtcNow.AddMinutes(-TrackingWindowMinutes))
            .ToList();

        return recentAttempts.Count;
    }
}
