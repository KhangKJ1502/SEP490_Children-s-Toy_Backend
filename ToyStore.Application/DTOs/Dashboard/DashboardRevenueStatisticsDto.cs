namespace ToyStore.Application.DTOs.Dashboard;

public class DashboardRevenueStatisticsDto
{
    public DashboardTimeRangeDto Range { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal PreviousPeriodRevenue { get; set; }
    public decimal GrowthPercentage { get; set; }
    public List<DashboardRevenueChartPointDto> Details { get; set; } = [];
}

public class DashboardRevenueChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
}
