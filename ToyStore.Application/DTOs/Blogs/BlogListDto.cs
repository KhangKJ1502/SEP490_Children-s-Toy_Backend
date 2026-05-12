namespace ToyStore.Application.DTOs.Blogs;

public class BlogListDto
{
    public int BlogPostId { get; set; }
    public short BlogCategoryId { get; set; }
    public string BlogCategoryName { get; set; } = string.Empty;
    public string BlogTitle { get; set; } = string.Empty;
    public string? BlogThumbnail { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public bool IsFeatured { get; set; }
    public int LikeCount { get; set; }
    public int LoveCount { get; set; }
    public int HahaCount { get; set; }
    public int CommentCount { get; set; }
    public int TotalInteraction { get; set; }
    public string? CurrentUserReaction { get; set; }
    public DateTime? BlogAt { get; set; }
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
