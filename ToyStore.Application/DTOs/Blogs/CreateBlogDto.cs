namespace ToyStore.Application.DTOs.Blogs;

public class CreateBlogDto
{
    public short BlogCategoryId { get; set; }
    public string BlogTitle { get; set; } = string.Empty;
    public string BlogContent { get; set; } = string.Empty;
    public string? BlogThumbnail { get; set; }
    public DateTime? BlogAt { get; set; }
}
