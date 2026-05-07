namespace ToyStore.Application.DTOs.Blogs;

public class UpdateBlogDto
{
    public short? BlogCategoryId { get; set; }
    public string? BlogTitle { get; set; }
    public string? BlogContent { get; set; }
    public string? BlogThumbnail { get; set; }
    public string? Status { get; set; }
    public DateTime? BlogAt { get; set; }
}
