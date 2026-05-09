namespace ToyStore.Application.DTOs.Reviews;

public class AdminReviewQueryDto
{
    public int PageNumber { get; set; } = 1;
    
    public int PageSize { get; set; } = 10;
    
    public string? SortBy { get; set; }
    
    public bool SortDesc { get; set; } = true;
    
    public string? ModerationStatus { get; set; }
    
    public int? ProductId { get; set; }
    
    public int? AccountId { get; set; }
    
    public int? OrderId { get; set; }
    
    public string? SearchTerm { get; set; }
    
    public DateTime? FromDate { get; set; }
    
    public DateTime? ToDate { get; set; }
    
    public bool? IsDeleted { get; set; }
}
