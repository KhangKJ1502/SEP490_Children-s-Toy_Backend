namespace ToyStore.Application.DTOs.Dashboard;

public class DashboardTimeRangePairDto
{
    public DashboardTimeRangePairDto(
        DashboardTimeRangeInternalDto current,
        DashboardTimeRangeInternalDto previous)
    {
        Current = current;
        Previous = previous;
    }

    public DashboardTimeRangeInternalDto Current { get; }
    public DashboardTimeRangeInternalDto Previous { get; }
}
