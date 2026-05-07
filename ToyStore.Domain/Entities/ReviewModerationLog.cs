using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class ReviewModerationLog
{
    public int LogId { get; set; }

    public string TargetType { get; set; } = null!;

    public int ReviewId { get; set; }

    public int? ImageId { get; set; }

    public string ModeratorType { get; set; } = null!;

    public int? ModeratedBy { get; set; }

    public string Action { get; set; } = null!;

    public string? AiModelVersion { get; set; }

    public string? ModerationResult { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ReviewProductImage? Image { get; set; }

    public virtual Account? ModeratedByNavigation { get; set; }

    public virtual ReviewProduct Review { get; set; } = null!;
}
