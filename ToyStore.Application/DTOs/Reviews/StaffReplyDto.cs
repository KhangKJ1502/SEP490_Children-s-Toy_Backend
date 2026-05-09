namespace ToyStore.Application.DTOs.Reviews;

public class StaffReplyDto
{
    public int ReplyProductId { get; set; }
    
    public int StaffId { get; set; }
    
    public string StaffName { get; set; } = null!;
    
    public string Content { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
}
