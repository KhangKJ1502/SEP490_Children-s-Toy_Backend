namespace ToyStore.Application.DTOs.Reviews;

public class ReviewLikeResponseDto
{
    public int ReviewId { get; set; }
    public int LikeCount { get; set; }
    public bool IsLiked { get; set; }
}
