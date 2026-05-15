namespace ToyStore.Application.DTOs.Products;

public class InventoryReportFileDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public string ContentType { get; set; } = "application/octet-stream";

    public string FileName { get; set; } = "inventory-report";
}
