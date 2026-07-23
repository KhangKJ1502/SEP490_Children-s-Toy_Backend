namespace ToyStore.Application.DTOs.Orders;

public class CancelOrderCustomerRequestDto
{
    public string? Reason { get; set; }
    public bool RestoreCart { get; set; } = false;
}
