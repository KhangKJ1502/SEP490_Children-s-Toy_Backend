namespace ToyStore.Application.DTOs.Assignments;

public sealed class OrderAssignmentTransferItem
{
    public int OrderId { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public string StatusName { get; set; } = string.Empty;
}
