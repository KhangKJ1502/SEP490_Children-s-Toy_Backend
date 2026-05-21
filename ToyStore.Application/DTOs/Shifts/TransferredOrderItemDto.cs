namespace ToyStore.Application.DTOs.Shifts;

public sealed class TransferredOrderItemDto
{
    public int OrderId { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public string StatusName { get; set; } = string.Empty;

    public int OldAccountId { get; set; }

    public int NewAccountId { get; set; }
}
