namespace ToyStore.Application.DTOs.Shifts;

public class ShiftTemplateDto
{
    public byte ShiftTemplateId { get; set; }

    public string ShiftName { get; set; } = string.Empty;

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public short MaxOrdersPerShift { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
