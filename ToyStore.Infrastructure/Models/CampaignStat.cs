using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class CampaignStat
{
    public int StatId { get; set; }

    public int CampaignId { get; set; }

    public int TotalSent { get; set; }

    public int TotalRead { get; set; }

    public int TotalClicked { get; set; }

    public DateTime ComputedAt { get; set; }

    public virtual Campaign Campaign { get; set; } = null!;
}
