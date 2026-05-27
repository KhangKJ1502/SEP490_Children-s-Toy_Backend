namespace ToyStore.Application.DTOs.Refunds;

public class CreateRefundItemDto
{
    public int ProductId { get; set; }
    public short Quantity { get; set; }
}
