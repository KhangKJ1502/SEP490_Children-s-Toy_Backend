namespace ToyStore.Application.DTOs.Orders;

/// <summary>
/// Request body cho PATCH /admin/orders/{id}/assign.
/// </summary>
public class AssignOrderRequestDto
{
    public int TargetAccountId { get; set; }
    public string? Note { get; set; }
}
