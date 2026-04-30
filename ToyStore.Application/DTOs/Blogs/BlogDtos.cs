namespace ToyStore.Application.DTOs.Blogs;

public class BlogListDto
{
    public int BlogPostId { get; set; }
    public short BlogCategoryId { get; set; }
    public string BlogTitle { get; set; } = string.Empty;
    public string? BlogThumbnail { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public DateTime? BlogAt { get; set; }
    public string Author { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class BlogDetailDto : BlogListDto
{
    public string BlogContent { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class CreateBlogDto
{
    public short BlogCategoryId { get; set; }
    public string BlogTitle { get; set; } = string.Empty;
    public string BlogContent { get; set; } = string.Empty;
    public string? BlogThumbnail { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? BlogAt { get; set; }
}

public class UpdateBlogDto
{
    public short BlogCategoryId { get; set; }
    public string BlogTitle { get; set; } = string.Empty;
    public string BlogContent { get; set; } = string.Empty;
    public string? BlogThumbnail { get; set; }
    public bool IsFeatured { get; set; }
    public string? Status { get; set; }
    public DateTime? BlogAt { get; set; }
}

public class SubmitBlogDto
{
    public string Status { get; set; } = string.Empty;
}

public class ApproveBlogDto
{
    public string Decision { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
