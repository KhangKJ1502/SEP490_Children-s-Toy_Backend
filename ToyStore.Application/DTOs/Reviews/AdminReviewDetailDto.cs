namespace ToyStore.Application.DTOs.Reviews;

public class AdminReviewDetailDto
{
    public int ReviewId { get; set; }
    
    public int AccountId { get; set; }
    
    public string AccountName { get; set; } = null!;
    
    public string AccountEmail { get; set; } = null!;
    
    public int ProductId { get; set; }
    
    public string ProductName { get; set; } = null!;
    
    public int OrderId { get; set; }
    
    public string OrderCode { get; set; } = null!;
    
    public byte Rating { get; set; }
    
    public string? Comment { get; set; }
    
    public string ModerationStatus { get; set; } = null!;
    
    public bool IsDeleted { get; set; }
    
    public bool IsEdited { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public List<ReviewImageDto> Images { get; set; } = new();
    
    public List<StaffReplyDto> Replies { get; set; } = new();
    
    public List<ModerationLogDto> ModerationLogs { get; set; } = new();
}
