namespace ToyStore.Application.Interfaces.Notifications;

public interface IUserPreferenceChecker
{
    /// <summary>
    /// Returns true if the account has opted in to the given preference.
    /// preferenceKey: "OrderUpdates" | "Promotions" | "StockAlerts" | "BlogAlerts"
    /// </summary>
    Task<bool> CanSendAsync(int accountId, string preferenceKey, CancellationToken ct = default);

    /// <summary>Email channel: requires EmailOptIn when preferences row exists.</summary>
    Task<bool> CanReceiveEmailAsync(int accountId, CancellationToken ct = default);

    /// <summary>Web push channel: requires WebPushOptIn when preferences row exists.</summary>
    Task<bool> CanReceiveWebPushAsync(int accountId, CancellationToken ct = default);
}
