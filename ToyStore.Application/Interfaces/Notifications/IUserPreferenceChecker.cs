namespace ToyStore.Application.Interfaces.Notifications;

public interface IUserPreferenceChecker
{
    /// <summary>
    /// Returns true if the account has opted in to the given preference.
    /// preferenceKey: "OrderUpdates" | "Promotions" | "StockAlerts" | "BlogAlerts"
    /// </summary>
    Task<bool> CanSendAsync(int accountId, string preferenceKey, CancellationToken ct = default);
}
