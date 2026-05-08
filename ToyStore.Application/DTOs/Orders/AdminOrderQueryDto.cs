namespace ToyStore.Application.DTOs.Orders;

/// <summary>
/// Query parameters cho GET /admin/orders.
/// </summary>
public class AdminOrderQueryDto
{
    public int? StatusId { get; set; }
    public bool AssignedToMe { get; set; } = false;
    public string? Keyword { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
