namespace ToyStore.Application.Common.Helpers;

/// <summary>
/// DateTime helper methods for Vietnam timezone.
/// </summary>
public static class DateTimeHelper
{
    private static readonly TimeZoneInfo VietnamTimeZone = 
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    
    /// <summary>
    /// Gets current Vietnam time.
    /// </summary>
    public static DateTime GetVietnamNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
    }
    
    /// <summary>
    /// Converts UTC to Vietnam time.
    /// </summary>
    public static DateTime ToVietnamTime(DateTime utcTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, VietnamTimeZone);
    }
    
    /// <summary>
    /// Converts Vietnam time to UTC.
    /// </summary>
    public static DateTime ToUtc(DateTime vietnamTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(vietnamTime, VietnamTimeZone);
    }
    
    /// <summary>
    /// Formats date for Vietnamese display.
    /// </summary>
    public static string FormatVietnamese(DateTime date)
    {
        return date.ToString("dd/MM/yyyy HH:mm");
    }
    
    /// <summary>
    /// Formats date as relative time (e.g., "2 hours ago").
    /// </summary>
    public static string ToRelativeTime(DateTime date)
    {
        var span = DateTime.UtcNow - date;
        
        if (span.TotalDays > 365)
            return $"{(int)(span.TotalDays / 365)} years ago";
        if (span.TotalDays > 30)
            return $"{(int)(span.TotalDays / 30)} months ago";
        if (span.TotalDays > 1)
            return $"{(int)span.TotalDays} days ago";
        if (span.TotalHours > 1)
            return $"{(int)span.TotalHours} hours ago";
        if (span.TotalMinutes > 1)
            return $"{(int)span.TotalMinutes} minutes ago";
        
        return "Just now";
    }
    
    /// <summary>
    /// Gets the start of day in Vietnam timezone.
    /// </summary>
    public static DateTime GetStartOfDay(DateTime date)
    {
        return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
    }
    
    /// <summary>
    /// Gets the end of day in Vietnam timezone.
    /// </summary>
    public static DateTime GetEndOfDay(DateTime date)
    {
        return new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, DateTimeKind.Unspecified);
    }
    
    /// <summary>
    /// Checks if a date is within business hours (8 AM - 6 PM).
    /// </summary>
    public static bool IsBusinessHours(DateTime dateTime)
    {
        var hour = dateTime.Hour;
        return hour >= 8 && hour < 18;
    }
}
