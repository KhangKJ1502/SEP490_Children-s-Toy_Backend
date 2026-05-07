namespace ToyStore.Application.DTOs.Blogs;

public class BlogDetailDto : BlogListDto
{
    public string BlogContent { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
