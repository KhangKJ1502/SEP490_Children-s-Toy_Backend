namespace ToyStore.Application.DTOs.Dashboard;

public static class DashboardTimePeriods
{
    public const string Today = "today";
    public const string CurrentWeek = "current_week";
    public const string PreviousWeek = "previous_week";
    public const string CurrentMonth = "current_month";
    public const string PreviousMonth = "previous_month";
    public const string CurrentQuarter = "current_quarter";
    public const string PreviousQuarter = "previous_quarter";

    public static readonly IReadOnlyCollection<string> Supported =
    [
        Today,
        CurrentWeek,
        PreviousWeek,
        CurrentMonth,
        PreviousMonth,
        CurrentQuarter,
        PreviousQuarter
    ];
}

public static class DashboardGroupByModes
{
    public const string Day = "day";
    public const string Week = "week";
    public const string Month = "month";

    public static readonly IReadOnlyCollection<string> Supported = [Day, Week, Month];
}

public class DashboardTimeFilterDto
{
    public string Period { get; set; } = DashboardTimePeriods.CurrentMonth;
    public string? GroupBy { get; set; }
}

public class DashboardTimeRangeDto
{
    public string Period { get; set; } = DashboardTimePeriods.CurrentMonth;
    public string GroupBy { get; set; } = DashboardGroupByModes.Day;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}
