namespace ToyStore.Application.DTOs.Shifts;

public sealed class UpdateWorkScheduleResultDto
{
    public WorkScheduleDto Schedule { get; set; } = null!;

    public int TransferredCount { get; set; }

    public int AutoAssignReassignedCount { get; set; }

    public int AutoAssignQueuedCount { get; set; }

    public DateTime TransferredAt { get; set; }

    public List<TransferredOrderItemDto> TransferredOrders { get; set; } = new();
}
