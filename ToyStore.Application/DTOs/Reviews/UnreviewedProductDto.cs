namespace ToyStore.Application.DTOs.Reviews;

public class UnreviewedProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? ProductImage { get; set; }
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = null!;
    public DateTime? CompletedAt { get; set; }
    public int RemainingDays { get; set; }
}
