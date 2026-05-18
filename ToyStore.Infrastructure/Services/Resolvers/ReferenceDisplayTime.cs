using System.Globalization;

namespace ToyStore.Infrastructure.Services.Resolvers;

/// <summary>
/// Định dạng mốc thời gian cho placeholders campaign (đọc được khi gửi user, đồng bộ với UI admin VN).
/// </summary>
internal static class ReferenceDisplayTime
{
    private static readonly TimeZoneInfo VietnamTz = CreateVietnamTz();

    private static TimeZoneInfo CreateVietnamTz()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    /// <summary>
    /// Giá trị từ DB được coi là UTC nếu Kind = Unspecified (pattern EF thường gặp).
    /// </summary>
    public static string FormatVietnamDateTime(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
        var vn = TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamTz);
        return vn.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"));
    }
}
