namespace ToyStore.Application.DTOs.Dashboard;

public class DashboardNewCustomerStatisticsDto
{
    public DashboardTimeRangeDto Range { get; set; } = new();
    public int TotalNewCustomers { get; set; }
    public int PreviousPeriodNewCustomers { get; set; }
    public decimal GrowthPercentage { get; set; }
    public List<DashboardCountChartPointDto> Details { get; set; } = [];
}

public class DashboardCountChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Value { get; set; }
}
