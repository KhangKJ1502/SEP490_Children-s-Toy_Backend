using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Services.Notifications;

/// <summary>
/// Kiểm tra UserPreferences trước khi gửi.
/// - WEB_BELL: không yêu cầu EmailOptIn; SYSTEM type luôn được gửi.
/// - EMAIL: yêu cầu EmailOptIn = 1 và cờ tương ứng theo NotificationType.
/// Nếu account chưa có bản ghi UserPreferences → mặc định cho phép gửi tất cả.
/// </summary>
public class NotificationPreferencesGate : INotificationPreferencesGate
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationPreferencesGate> _logger;

    public NotificationPreferencesGate(IUnitOfWork unitOfWork, ILogger<NotificationPreferencesGate> logger)
    {
        _unitOfWork = unitOfWork;
        _logger     = logger;
    }

    public async Task<bool> CanSendAsync(
        int accountId,
        string channel,
        string notificationType,
        CancellationToken ct = default)
    {
        // Thông báo SYSTEM (lỗi hệ thống, outbox stuck…) luôn được gửi
        if (notificationType == NotificationTypes.System)
            return true;

        var prefs = await _unitOfWork.UserPreferences.GetByAccountIdAsync(accountId, ct);

        // Chưa có bản ghi preferences → Email phải được BẬT mới mở gửi; Web mặc định được gửi
        if (prefs is null)
        {
            if (channel == NotificationChannels.Email)
                return false;
            return true;
        }

        // Kiểm tra cờ theo NotificationType
        var topicAllowed = notificationType switch
        {
            NotificationTypes.Order     => prefs.OrderUpdates,
            NotificationTypes.Promotion => prefs.Promotions,
            NotificationTypes.Stock     => prefs.StockAlerts,
            NotificationTypes.Blog      => prefs.BlogAlerts,
            _                           => true,
        };

        if (!topicAllowed)
        {
            _logger.LogDebug(
                "Notification suppressed (topic opt-out). AccountID={Id} Type={Type} Channel={Channel}",
                accountId, notificationType, channel);
            return false;
        }

        // EMAIL cần thêm cờ EmailOptIn
        if (channel == NotificationChannels.Email && !prefs.EmailOptIn)
        {
            _logger.LogDebug(
                "Email notification suppressed (EmailOptIn=0). AccountID={Id} Type={Type}",
                accountId, notificationType);
            return false;
        }

        // WEB_PUSH cần thêm cờ WebPushOptIn (WEB_BELL là chuông trong app, không bị chặn bởi cờ WebPush)
        if (channel == NotificationChannels.WebPush && !prefs.WebPushOptIn)
        {
            _logger.LogDebug(
                "Web push notification suppressed (WebPushOptIn=0). AccountID={Id} Type={Type} Channel={Channel}",
                accountId, notificationType, channel);
            return false;
        }

        return true;
    }
}
