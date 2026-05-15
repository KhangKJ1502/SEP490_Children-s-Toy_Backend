namespace ToyStore.Application.DTOs.Shifts;

public class CreateShiftTemplateDto
{
    public string ShiftName { get; set; } = string.Empty;

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public short MaxOrdersPerShift { get; set; } = 20;

    public bool IsActive { get; set; } = true;
}
