namespace ToyStore.Application.DTOs.Reviews;

public class UpdateModerationStatusDto
{
    public string? ModerationStatus { get; set; }
    
    public string? Reason { get; set; }

    public bool? IsDeleted { get; set; }
}
