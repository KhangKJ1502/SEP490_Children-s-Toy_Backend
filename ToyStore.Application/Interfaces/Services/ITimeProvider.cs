namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Provides a consistent way to access current time across the system.
/// Encourages UTC-first storage while providing Vietnam local time for display/scheduling.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Current system time in UTC. Always use this for database storage and internal logic.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Current time in Vietnam (UTC+7). Use this for display or logic specific to Vietnam timezone.
    /// </summary>
    DateTime VnNow { get; }

    /// <summary>
    /// Current date in Vietnam (UTC+7) at midnight. Useful for daily scheduling.
    /// </summary>
    DateTime TodayVn { get; }

    /// <summary>
    /// Converts a UTC DateTime to Vietnam time.
    /// </summary>
    DateTime ToVnTime(DateTime utcTime);

    /// <summary>
    /// Converts a local Vietnam time to UTC.
    /// </summary>
    DateTime ToUtc(DateTime vnTime);
}
