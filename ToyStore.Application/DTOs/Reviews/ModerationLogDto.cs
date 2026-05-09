namespace ToyStore.Application.DTOs.Reviews;

public class ModerationLogDto
{
    public int LogId { get; set; }
    
    public string TargetType { get; set; } = null!;
    
    public int? ImageId { get; set; }
    
    public string ModeratorType { get; set; } = null!;
    
    public string? ModeratedByName { get; set; }
    
    public string Action { get; set; } = null!;
    
    public string? Reason { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
