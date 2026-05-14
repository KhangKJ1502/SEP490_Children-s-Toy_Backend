namespace ToyStore.Application.DTOs.Assignments;

public class OrderQueueItemDto
{
    public int QueueId { get; set; }

    public int OrderId { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public DateTime QueuedAt { get; set; }

    public string Reason { get; set; } = string.Empty;

    public bool IsResolved { get; set; }

    public DateTime? ResolvedAt { get; set; }
}
