using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Delivery
{
    public long DeliveryId { get; set; }

    public int AccountId { get; set; }

    public int? CreatedByJobId { get; set; }

    public string TemplateCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual BackgroundJob? CreatedByJob { get; set; }

    public virtual Template TemplateCodeNavigation { get; set; } = null!;
}
