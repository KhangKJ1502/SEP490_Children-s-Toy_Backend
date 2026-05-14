namespace ToyStore.Application.DTOs.Dashboard;

public class DashboardOrderStatusStatisticsDto
{
    public DashboardTimeRangeDto Range { get; set; } = new();
    public int TotalOrders { get; set; }
    public List<DashboardOrderStatusItemDto> Statuses { get; set; } = [];
    public List<DashboardOrderStatusTimelinePointDto> Details { get; set; } = [];
}

public class DashboardOrderStatusItemDto
{
    public string Status { get; set; } = string.Empty;
    public int Value { get; set; }
    public decimal Percentage { get; set; }
}

public class DashboardOrderStatusTimelinePointDto
{
    public string Label { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Value { get; set; }
}
