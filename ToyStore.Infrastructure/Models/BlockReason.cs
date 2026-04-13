using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class BlockReason
{
    public short ReasonId { get; set; }

    public string? ReasonCode { get; set; }

    public string Content { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<UserBlockHistory> UserBlockHistories { get; set; } = new List<UserBlockHistory>();
}
