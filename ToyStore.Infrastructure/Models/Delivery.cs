using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Delivery
{
    public long DeliveryId { get; set; }

    public int AccountId { get; set; }

    public int? CreatedByJobId { get; set; }

    public string TemplateCode { get; set; } = null!;

    public string RecipientType { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? ImageUrl { get; set; }

    public string? ActionType { get; set; }

    public string? ActionTarget { get; set; }

    public int? CampaignId { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Campaign? Campaign { get; set; }

    public virtual BackgroundJob? CreatedByJob { get; set; }

    public virtual ICollection<DeliveryAction> DeliveryActions { get; set; } = new List<DeliveryAction>();

    public virtual Template TemplateCodeNavigation { get; set; } = null!;
}
