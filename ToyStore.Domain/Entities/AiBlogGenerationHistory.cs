using System;

namespace ToyStore.Domain.Entities;

public partial class AiBlogGenerationHistory
{
    public long HistoryId { get; set; }

    public int BlogPostId { get; set; }

    public int? QueueId { get; set; }

    public int StaffId { get; set; }

    public int? TemplateId { get; set; }

    public string PromptData { get; set; } = null!;

    public string? ModelName { get; set; }

    public decimal? Temperature { get; set; }

    public int? MaxTokens { get; set; }

    public string? Language { get; set; }

    public string? Tone { get; set; }

    public string? GeneratedContent { get; set; }

    public string? ContentHash { get; set; }

    public int? TokenInput { get; set; }

    public int? TokenOutput { get; set; }

    public int? LatencyMs { get; set; }

    public string Status { get; set; } = null!;

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public string? CorrelationId { get; set; }

    public string? IdempotencyKey { get; set; }

    public bool IsAppliedToBlog { get; set; }

    public DateTime? AppliedAt { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual BlogPost BlogPost { get; set; } = null!;

    public virtual AiBlogQueue? Queue { get; set; }

    public virtual Account Staff { get; set; } = null!;

    public virtual AiPromptTemplate? Template { get; set; }
}
