using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Services.Notifications;

public class UserPreferenceChecker : IUserPreferenceChecker
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserPreferenceChecker> _logger;

    public UserPreferenceChecker(IUnitOfWork unitOfWork, ILogger<UserPreferenceChecker> logger)
    {
        _unitOfWork = unitOfWork;
        _logger     = logger;
    }

    public async Task<bool> CanSendAsync(int accountId, string preferenceKey, CancellationToken ct = default)
    {
        var prefs = await _unitOfWork.UserPreferences.GetByAccountIdAsync(accountId, ct);

        // If no preference record exists default to opted-in
        if (prefs is null)
            return true;

        var allowed = preferenceKey switch
        {
            PreferenceKeys.OrderUpdates => prefs.OrderUpdates && prefs.EmailOptIn,
            PreferenceKeys.Promotions   => prefs.Promotions,
            PreferenceKeys.StockAlerts  => prefs.StockAlerts,
            PreferenceKeys.BlogAlerts   => prefs.BlogAlerts,
            _                           => true,
        };

        if (!allowed)
            _logger.LogWarning(
                "Notification suppressed for AccountID={AccountId} preferenceKey={Key}",
                accountId, preferenceKey);

        return allowed;
    }
}
