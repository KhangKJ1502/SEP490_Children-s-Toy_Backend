namespace ToyStore.Application.DTOs.Shifts;

public class UpdateShiftTemplateDto
{
    public string? ShiftName { get; set; }

    public TimeSpan? StartTime { get; set; }

    public TimeSpan? EndTime { get; set; }

    public short? MaxOrdersPerShift { get; set; }

    public bool? IsActive { get; set; }
}
