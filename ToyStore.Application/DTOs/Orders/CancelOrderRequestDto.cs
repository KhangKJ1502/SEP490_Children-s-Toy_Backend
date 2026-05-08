namespace ToyStore.Application.DTOs.Orders;

/// <summary>
/// Request body cho PATCH /admin/orders/{id}/cancel.
/// </summary>
public class CancelOrderRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Response cho cancel order.
/// </summary>
public class CancelOrderResponseDto
{
    public int OrderId { get; set; }
    public DateTime CancelledAt { get; set; }
}
