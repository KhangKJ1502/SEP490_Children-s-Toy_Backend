namespace ToyStore.Application.DTOs.Shifts;

public class TransferLoadRequestDto
{
    public int TargetScheduleId { get; set; }

    public string? Note { get; set; }
}
