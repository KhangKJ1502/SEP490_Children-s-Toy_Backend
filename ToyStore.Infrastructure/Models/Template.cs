using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Template
{
    public short TemplateId { get; set; }

    public string TemplateCode { get; set; } = null!;

    public string TitleTemplate { get; set; } = null!;

    public string MessageTemplate { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
