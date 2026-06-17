namespace ToyStore.Application.DTOs.Products;

public class ProductQuantityReportRequestDto
{
    public string Format { get; set; } = "pdf";

    public string? SearchTerm { get; set; }

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }

    public short? CategoryId { get; set; }

    public int? BrandId { get; set; }

    public byte? PriceRangeId { get; set; }

    public short? MaterialId { get; set; }

    public byte? AgeId { get; set; }

    public byte? OriginId { get; set; }

    public string? Status { get; set; }

    public bool LowStockOnly { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public string? DateField { get; set; }
}
