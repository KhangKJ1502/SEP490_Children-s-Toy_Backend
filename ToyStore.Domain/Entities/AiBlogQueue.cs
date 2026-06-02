using System;

namespace ToyStore.Domain.Entities;

public partial class AiBlogQueue
{
    public int QueueId { get; set; }

    public int BlogPostId { get; set; }

    public int StaffId { get; set; }

    public int? TemplateId { get; set; }

    public string PromptData { get; set; } = null!;

    public string? GeneratedContent { get; set; }

    public string Status { get; set; } = null!;

    public int Priority { get; set; }

    public int RetryCount { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual BlogPost BlogPost { get; set; } = null!;

    public virtual Account Staff { get; set; } = null!;

    public virtual AiPromptTemplate? Template { get; set; }
}
