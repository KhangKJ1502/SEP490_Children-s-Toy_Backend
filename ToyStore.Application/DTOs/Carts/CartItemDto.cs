namespace ToyStore.Application.DTOs.Carts;

public class CartItemDto
{
    public int CartItemId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductStatus { get; set; } = string.Empty;

    public string? MainImageUrl { get; set; }

    public short Quantity { get; set; }

    public int StockQuantity { get; set; }

    public decimal PriceAtThatTime { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal LineTotal { get; set; }

    public bool IsSelected { get; set; }

    public DateTime AddedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? WarningMessage { get; set; }
}
