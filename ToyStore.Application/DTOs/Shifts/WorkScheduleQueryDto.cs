namespace ToyStore.Application.DTOs.Shifts;

public class WorkScheduleQueryDto
{
    public DateTime? WorkDate { get; set; }

    public string? Status { get; set; }

    public byte? RoleId { get; set; }
}
