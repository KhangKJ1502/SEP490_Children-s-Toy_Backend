namespace ToyStore.Application.DTOs.Reviews;

public class ReviewQueryDto
{
    public int ProductId { get; set; }
    
    public int PageNumber { get; set; } = 1;
    
    public int PageSize { get; set; } = 10;
    
    public string? SortBy { get; set; }
    
    public bool SortDesc { get; set; } = true;
    
    public byte? Rating { get; set; }
    
    public bool? HasImage { get; set; }
    
    public string? SearchTerm { get; set; }
}
