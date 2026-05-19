namespace ToyStore.Application.DTOs.Dashboard;

public class DashboardTopSellingProductsDto
{
    public int Limit { get; set; }
    public int TotalItems { get; set; }
    public List<DashboardTopSellingProductItemDto> Products { get; set; } = [];
}

public class DashboardTopSellingProductItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int TotalSold { get; set; }
    public decimal Revenue { get; set; }
}

public class DashboardSlowMovingProductsDto
{
    public int Limit { get; set; }
    public int TotalItems { get; set; }
    public List<DashboardSlowMovingProductItemDto> Products { get; set; } = [];
}

public class DashboardSlowMovingProductItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int QuantityInStock { get; set; }
    public DateTime StockedAt { get; set; }
    public int DaysInStock { get; set; }
}

public class DashboardTotalProductsDto
{
    public int TotalProducts { get; set; }
}
