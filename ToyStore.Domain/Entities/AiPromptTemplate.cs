using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class AiPromptTemplate
{
    public int TemplateId { get; set; }

    public string TemplateName { get; set; } = null!;

    public string? Description { get; set; }

    public string PromptStructure { get; set; } = null!;

    public string? DefaultTone { get; set; }

    public short? DefaultCategoryId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual BlogCategory? DefaultCategory { get; set; }

    public virtual ICollection<AiBlogQueue> AiBlogQueues { get; set; } = new List<AiBlogQueue>();

    public virtual ICollection<AiBlogGenerationHistory> AiBlogGenerationHistories { get; set; } = new List<AiBlogGenerationHistory>();
}
