namespace ToyStore.Application.DTOs.Blogs;

public class ApproveBlogDto
{
    public string Decision { get; set; } = string.Empty;
    public bool? PublishNow { get; set; }
    public string? Reason { get; set; }
}
