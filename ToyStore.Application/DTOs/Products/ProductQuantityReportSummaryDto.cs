namespace ToyStore.Application.DTOs.Products;

public class ProductQuantityReportSummaryDto
{
    public int TotalProducts { get; set; }

    public int TotalQuantity { get; set; }

    public decimal TotalProductValue { get; set; }

    public int LowStockCount { get; set; }

    public int OutOfStockCount { get; set; }
}
