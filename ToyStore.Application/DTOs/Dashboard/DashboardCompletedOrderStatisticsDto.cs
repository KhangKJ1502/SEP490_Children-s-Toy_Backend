namespace ToyStore.Application.DTOs.Dashboard;

public class DashboardCompletedOrderStatisticsDto
{
    public DashboardTimeRangeDto Range { get; set; } = new();
    public int TotalCompletedOrders { get; set; }
    public int PreviousPeriodCompletedOrders { get; set; }
    public decimal GrowthPercentage { get; set; }
    public List<DashboardCountChartPointDto> Details { get; set; } = [];
}
