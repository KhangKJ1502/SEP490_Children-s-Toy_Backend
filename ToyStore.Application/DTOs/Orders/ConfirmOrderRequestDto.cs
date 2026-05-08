namespace ToyStore.Application.DTOs.Orders;

/// <summary>
/// Request body cho PATCH /admin/orders/{id}/confirm.
/// </summary>
public class ConfirmOrderRequestDto
{
    public string? Note { get; set; }
}

/// <summary>
/// Response cho confirm order.
/// </summary>
public class ConfirmOrderResponseDto
{
    public int OrderId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime ConfirmedAt { get; set; }
}
