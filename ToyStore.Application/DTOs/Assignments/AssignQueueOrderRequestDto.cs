namespace ToyStore.Application.DTOs.Assignments;

public class AssignQueueOrderRequestDto
{
    public int StaffScheduleId { get; set; }

    public int MerchScheduleId { get; set; }

    public string? Notes { get; set; }
}
