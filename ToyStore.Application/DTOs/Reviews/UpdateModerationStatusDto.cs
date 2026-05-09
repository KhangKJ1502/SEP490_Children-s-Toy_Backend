namespace ToyStore.Application.DTOs.Reviews;

public class UpdateModerationStatusDto
{
    public string ModerationStatus { get; set; } = null!;
    
    public string? Reason { get; set; }
}
