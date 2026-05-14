namespace ToyStore.Application.DTOs.Dashboard;

public class DashboardOrderRateStatisticsDto
{
    public DashboardTimeRangeDto Range { get; set; } = new();
    public int TotalOrders { get; set; }
    public int RefundedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal RefundRatePercentage { get; set; }
    public decimal CancellationRatePercentage { get; set; }
}
