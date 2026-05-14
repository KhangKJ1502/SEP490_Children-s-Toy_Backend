using ToyStore.Domain.Constants;

namespace ToyStore.Application.DTOs.Dashboard;

public class DashboardTimeRangeInternalDto
{
    public string Period { get; set; } = DashboardTimePeriods.CurrentMonth;
    public string GroupBy { get; set; } = DashboardGroupByModes.Day;
    public DateTime StartVn { get; set; }
    public DateTime EndVnExclusive { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtcExclusive { get; set; }
}
