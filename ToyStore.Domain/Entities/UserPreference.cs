using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class UserPreference
{
    public int PreferenceId { get; set; }

    public int AccountId { get; set; }

    public bool EmailOptIn { get; set; }

    public bool WebPushOptIn { get; set; }

    public bool OrderUpdates { get; set; }

    public bool Promotions { get; set; }

    public bool StockAlerts { get; set; }

    public bool BlogAlerts { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
