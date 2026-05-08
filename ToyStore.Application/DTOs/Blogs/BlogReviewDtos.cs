namespace ToyStore.Application.DTOs.Blogs;

public class BlogReviewDto
{
    public int ReviewBlogId { get; set; }
    public int BlogPostId { get; set; }
    public string BlogTitle { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? AccountImageUrl { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string Status { get; set; } = "Visible";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BlogReviewReplyDto> Replies { get; set; } = new();
}

public class BlogReviewReplyDto
{
    public int ReplyBlogId { get; set; }
    public int ReviewBlogId { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? AccountImageUrl { get; set; }
    public int? ParentReplyId { get; set; }
    public int? ReplyToAccountId { get; set; }
    public string? ReplyToAccountName { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string Status { get; set; } = "Visible";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BlogReviewReplyDto> Replies { get; set; } = new();
}

public class CreateBlogReviewDto
{
    public string Comment { get; set; } = string.Empty;
}

public class CreateBlogReviewReplyDto
{
    public string Comment { get; set; } = string.Empty;
    public int? ParentReplyId { get; set; }
    public int? ReplyToAccountId { get; set; }
}

public class UpdateBlogReviewStatusDto
{
    public string Status { get; set; } = string.Empty;
}
