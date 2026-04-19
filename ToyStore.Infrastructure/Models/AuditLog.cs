using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class AuditLog
{
    public long AuditId { get; set; }

    public string EntityType { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string Action { get; set; } = null!;

    public int PerformedBy { get; set; }

    public string? Ipaddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account PerformedByNavigation { get; set; } = null!;
}
