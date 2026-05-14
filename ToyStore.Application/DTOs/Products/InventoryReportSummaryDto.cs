namespace ToyStore.Application.DTOs.Products;

public class InventoryReportSummaryDto
{
    public int TotalProducts { get; set; }

    public int TotalQuantity { get; set; }

    public decimal TotalInventoryValue { get; set; }

    public int LowStockCount { get; set; }

    public int OutOfStockCount { get; set; }
}
