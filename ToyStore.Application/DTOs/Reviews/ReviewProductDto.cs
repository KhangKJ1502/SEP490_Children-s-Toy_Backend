namespace ToyStore.Application.DTOs.Reviews;

public class ReviewProductDto
{
    public int ReviewId { get; set; }
    
    public int AccountId { get; set; }
    
    public int ProductId { get; set; }
    
    public int OrderId { get; set; }
    
    public byte Rating { get; set; }
    
    public string? Comment { get; set; }
    
    public string ModerationStatus { get; set; } = null!;
    
    public bool IsEdited { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public List<ReviewImageDto> Images { get; set; } = new();
    
    public List<StaffReplyDto> Replies { get; set; } = new();
}
