namespace ToyStore.Application.DTOs.Reviews;

public class MyReviewDto
{
    public int ReviewId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? ProductImage { get; set; }
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = null!;
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public string ModerationStatus { get; set; } = null!;
    public bool IsEdited { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public List<ReviewImageDto> Images { get; set; } = new();
    public List<StaffReplyDto> Replies { get; set; } = new();
}
