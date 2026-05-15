namespace ToyStore.Application.DTOs.Shifts;

public class WorkScheduleListDto
{
    public int ScheduleId { get; set; }

    public int AccountId { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public byte RoleId { get; set; }

    public byte ShiftTemplateId { get; set; }

    public string ShiftName { get; set; } = string.Empty;

    public DateTime WorkDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public short CurrentLoad { get; set; }

    public short MaxLoad { get; set; }
}
