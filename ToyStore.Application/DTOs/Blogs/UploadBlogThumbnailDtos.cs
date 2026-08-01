namespace ToyStore.Application.DTOs.Blogs;

/// <summary>
/// Response trả về sau khi upload thumbnail blog thành công.
/// </summary>
public class UploadBlogThumbnailResponse
{
    public string Url { get; set; } = string.Empty;
}
