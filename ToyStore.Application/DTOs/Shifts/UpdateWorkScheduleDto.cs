namespace ToyStore.Application.DTOs.Shifts;

public class UpdateWorkScheduleDto
{
    public int AccountId { get; set; }
    public byte ShiftTemplateId { get; set; }
    public DateTime WorkDate { get; set; }
    public string? Status { get; set; }
    public short? MaxLoadOverride { get; set; }

    /// <summary>Optional optimistic concurrency: must match WorkSchedules.UpdatedAt (UTC).</summary>
    public DateTime? ExpectedUpdatedAt { get; set; }

    /// <summary>
    /// When swapping account on a shift: after direct transfer, optionally deactivate
    /// remaining assignments on this schedule and run AutoAssign per order.
    /// </summary>
    public bool RunAutoAssignFallback { get; set; }
}
