using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class BlogCommentBanReason
{
    public byte BanReasonId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BlogCommentModerationLog> BlogCommentModerationLogs { get; set; } = new List<BlogCommentModerationLog>();
}
