namespace ToyStore.Application.DTOs.Blogs;

public sealed class PythonBlogGenerateRequest
{
    public string Action { get; set; } = "Generate";

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string PromptStructure { get; set; } = string.Empty;

    public string DefaultTone { get; set; } = "Friendly";

    public int DefaultCategoryId { get; set; }

    public string? SourceContent { get; set; }
}
