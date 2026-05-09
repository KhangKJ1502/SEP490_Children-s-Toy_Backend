namespace ToyStore.Application.DTOs.Reviews;

public class ReviewProductListDto
{
    public int ReviewId { get; set; }
    
    public int ProductId { get; set; }
    
    public string ReviewerName { get; set; } = null!;
    
    public string? ReviewerAvatarUrl { get; set; }
    
    public byte Rating { get; set; }
    
    public string? Comment { get; set; }
    
    public bool IsEdited { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public List<ReviewImageDto> Images { get; set; } = new();
    
    public List<StaffReplyDto> Replies { get; set; } = new();
}
