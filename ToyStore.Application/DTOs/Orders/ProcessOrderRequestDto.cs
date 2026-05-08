namespace ToyStore.Application.DTOs.Orders;

/// <summary>
/// Request body cho PATCH /admin/orders/{id}/process.
/// </summary>
public class ProcessOrderRequestDto
{
    public string? Note { get; set; }
}

/// <summary>
/// Response cho process order.
/// </summary>
public class ProcessOrderResponseDto
{
    public int OrderId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}
