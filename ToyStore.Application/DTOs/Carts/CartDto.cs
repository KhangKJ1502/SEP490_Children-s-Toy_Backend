namespace ToyStore.Application.DTOs.Carts;

public class CartDto
{
    public int CartId { get; set; }

    public int AccountId { get; set; }

    public int TotalItem { get; set; }

    public int TotalQuantity { get; set; }

    public decimal SubTotal { get; set; }

    public List<CartItemDto> Items { get; set; } = [];
}
