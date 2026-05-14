namespace ToyStore.Application.DTOs.Assignments;

public class ReassignOrderRequestDto
{
    public byte RoleId { get; set; }

    public int NewScheduleId { get; set; }

    public string? Notes { get; set; }
}
