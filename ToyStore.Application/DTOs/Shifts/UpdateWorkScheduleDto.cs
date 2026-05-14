namespace ToyStore.Application.DTOs.Shifts;

public class UpdateWorkScheduleDto
{
    public int AccountId { get; set; }
    public byte ShiftTemplateId { get; set; }
    public DateTime WorkDate { get; set; }
    public string? Status { get; set; }
    public short? MaxLoadOverride { get; set; }
}
