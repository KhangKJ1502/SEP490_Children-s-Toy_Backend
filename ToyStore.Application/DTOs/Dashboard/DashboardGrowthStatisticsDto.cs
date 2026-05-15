namespace ToyStore.Application.DTOs.Dashboard;

public class DashboardGrowthStatisticsDto
{
    public DashboardTimeRangeDto Range { get; set; } = new();
    public decimal RevenueCurrent { get; set; }
    public decimal RevenuePrevious { get; set; }
    public decimal RevenueGrowthPercentage { get; set; }
    public int OrdersCurrent { get; set; }
    public int OrdersPrevious { get; set; }
    public decimal OrdersGrowthPercentage { get; set; }
}
