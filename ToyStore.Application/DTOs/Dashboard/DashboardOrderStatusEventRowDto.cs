namespace ToyStore.Application.DTOs.Dashboard;

public class DashboardOrderStatusEventRowDto
{
    public DateTime OrderDateUtc { get; set; }
    public string Status { get; set; } = string.Empty;
}
