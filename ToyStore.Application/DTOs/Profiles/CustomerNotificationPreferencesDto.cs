namespace ToyStore.Application.DTOs.Profiles;

/// <summary>
/// Customer-facing snapshot of Notification.UserPreferences row for the logged-in account.
/// </summary>
public class CustomerNotificationPreferencesDto
{
    public bool EmailOptIn { get; set; }

    public bool WebPushOptIn { get; set; }

    public bool OrderUpdates { get; set; }

    public bool Promotions { get; set; }

    public bool StockAlerts { get; set; }

    public bool BlogAlerts { get; set; }
}

/// <summary>
/// Full update body for PUT (all toggles).
/// </summary>
public class UpdateCustomerNotificationPreferencesDto
{
    public bool EmailOptIn { get; set; }

    public bool WebPushOptIn { get; set; }

    public bool OrderUpdates { get; set; }

    public bool Promotions { get; set; }

    public bool StockAlerts { get; set; }

    public bool BlogAlerts { get; set; }
}
