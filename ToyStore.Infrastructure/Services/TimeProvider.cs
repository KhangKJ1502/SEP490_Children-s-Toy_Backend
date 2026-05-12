using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

public class TimeProvider : ITimeProvider
{
    private static readonly TimeZoneInfo VnTimeZone = 
        TimeZoneInfo.CreateCustomTimeZone("Vietnam Standard Time", new TimeSpan(7, 0, 0), "Vietnam Time", "Vietnam Time");

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime VnNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VnTimeZone);

    public DateTime TodayVn => VnNow.Date;

    public DateTime ToVnTime(DateTime utcTime)
    {
        if (utcTime.Kind == DateTimeKind.Unspecified)
        {
            utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, VnTimeZone);
    }

    public DateTime ToUtc(DateTime vnTime)
    {
        if (vnTime.Kind == DateTimeKind.Utc)
        {
            return vnTime;
        }
        if (vnTime.Kind == DateTimeKind.Local)
        {
            return vnTime.ToUniversalTime();
        }
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(vnTime, DateTimeKind.Unspecified), VnTimeZone);
    }
}
