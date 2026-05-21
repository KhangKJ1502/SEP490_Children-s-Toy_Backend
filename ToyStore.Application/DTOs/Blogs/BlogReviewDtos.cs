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
    public string ModerationStatus { get; set; } = "Pending";
    public bool IsHidden { get; set; }
    public byte? BanReasonId { get; set; }
    public string? BanReasonContent { get; set; }
    public int LikeCount { get; set; }
    public int LoveCount { get; set; }
    public int HahaCount { get; set; }
    public string? CurrentUserReaction { get; set; }
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
    public string ModerationStatus { get; set; } = "Pending";
    public byte? BanReasonId { get; set; }
    public string? BanReasonContent { get; set; }
    public int LikeCount { get; set; }
    public int LoveCount { get; set; }
    public int HahaCount { get; set; }
    public string? CurrentUserReaction { get; set; }
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
    public string ModerationStatus { get; set; } = string.Empty;
    public byte? BanReasonId { get; set; }
}

public class BlogCommentBanReasonDto
{
    public byte BanReasonId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class BlogReviewPermissionDto
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AccountImageUrl { get; set; }
    public byte ViolationCount { get; set; }
    public bool IsCommentBanned { get; set; }
    public DateTime? BannedAt { get; set; }
    public DateTime? BanExpiresAt { get; set; }
    public DateTime? UnbannedAt { get; set; }
    public int? UnbannedBy { get; set; }
    public string? UnbannedByName { get; set; }
    public DateTime? LastViolatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateBlogReviewPermissionDto
{
    public bool IsCommentBanned { get; set; }
}

public class UpsertReactionDto
{
    public string ReactionCode { get; set; } = string.Empty;
}

public class ReactionSummaryDto
{
    public int LikeCount { get; set; }
    public int LoveCount { get; set; }
    public int HahaCount { get; set; }
    public int TotalCount { get; set; }
    public string? CurrentUserReaction { get; set; }
}
