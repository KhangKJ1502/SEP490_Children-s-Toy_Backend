namespace ToyStore.Application.DTOs.Shifts;

public class ShiftTemplateListDto
{
    public byte ShiftTemplateId { get; set; }

    public string ShiftName { get; set; } = string.Empty;

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public short MaxOrdersPerShift { get; set; }

    public bool IsActive { get; set; }
}
