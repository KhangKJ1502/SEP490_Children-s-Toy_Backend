using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class CampaignTarget
{
    public int CampaignTargetId { get; set; }

    public int CampaignId { get; set; }

    public string TargetType { get; set; } = null!;

    public string TargetValue { get; set; } = null!;

    public virtual Campaign Campaign { get; set; } = null!;
}
