namespace ToyStore.Application.DTOs.Reviews;

public class MyReviewQueryDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; } = true;
    public string? ModerationStatus { get; set; }
}
