namespace ToyStore.Application.DTOs.Shifts;

public sealed class MarkAbsentResultDto
{
    public int ReassignedCount { get; set; }

    public int QueuedCount { get; set; }

    public List<int> AffectedPendingOrderIds { get; set; } = new();
}
