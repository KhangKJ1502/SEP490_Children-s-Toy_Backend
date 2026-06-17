namespace ToyStore.Application.DTOs.Products;

public class ProductQuantityReportFileDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public string ContentType { get; set; } = "application/octet-stream";

    public string FileName { get; set; } = "product-quantity-report";
}
