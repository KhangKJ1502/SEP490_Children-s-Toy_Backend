namespace ToyStore.Application.DTOs.Blogs;

/// <summary>
/// Request body gửi lên để generate bài blog bằng AI.
/// </summary>
public class AiBlogGenerateRequest
{
    public int? BlogPostId { get; set; }
    public string? Action { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PromptStructure { get; set; } = string.Empty;
    public string? DefaultTone { get; set; }
    public int DefaultCategoryId { get; set; }
    public bool? IsActive { get; set; }
    public string? SourceContent { get; set; }
}

/// <summary>
/// Kết quả trả về sau khi AI generate bài blog thành công.
/// </summary>
public class AiBlogGenerateResult
{
    public int BlogPostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string BlogContent { get; set; } = string.Empty;
    public int BlogCategoryId { get; set; }
    public string PromptData { get; set; } = string.Empty;
    public string AiStatus { get; set; } = "Success";
    public string? AiError { get; set; }
}
