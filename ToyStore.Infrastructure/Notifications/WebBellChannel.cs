using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Notifications;

/// <summary>
/// Kênh WEB_BELL: INSERT Delivery vào DB, sau đó push realtime qua SignalR hub.
/// </summary>
public class WebBellChannel : INotificationChannel
{
    public string Channel => NotificationChannels.WebBell;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationHubService _hub;
    private readonly ILogger<WebBellChannel> _logger;
    private readonly ITimeProvider _timeProvider;

    public WebBellChannel(
        IUnitOfWork unitOfWork,
        INotificationHubService hub,
        ILogger<WebBellChannel> logger,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _hub        = hub;
        _logger     = logger;
        _timeProvider = timeProvider;
    }

    public async Task SendAsync(NotificationDeliveryRequest request, CancellationToken ct = default)
    {
        var title = request.Title ?? string.Empty;
        if (title.Length > 255)
        {
            title = title.Substring(0, 252) + "...";
        }

        var message = request.Message ?? string.Empty;
        if (message.Length > 2000)
        {
            message = message.Substring(0, 1997) + "...";
        }

        var actionTarget = request.ActionTarget;
        if (actionTarget != null && actionTarget.Length > 500)
        {
            actionTarget = actionTarget.Substring(0, 500);
        }

        var idempotencyKey = request.IdempotencyKey;
        if (idempotencyKey != null && idempotencyKey.Length > 200)
        {
            idempotencyKey = idempotencyKey.Substring(0, 200);
        }

        var delivery = new Delivery
        {
            AccountId        = request.AccountId,
            RecipientType    = request.RecipientType,
            Channel          = NotificationChannels.WebBell,
            NotificationType = request.NotificationType,
            Title            = title,
            Message          = message,
            Payload          = request.PayloadJson ?? "{}",
            Status           = NotificationStatuses.Unread,
            TemplateCode     = request.TemplateCode,
            ImageUrl         = request.ImageUrl,
            ActionType       = request.ActionType,
            ActionTarget     = actionTarget,
            IdempotencyKey   = idempotencyKey,
            CampaignId       = request.CampaignId,
            CreatedAt        = _timeProvider.UtcNow,
        };

        try
        {
            _unitOfWork.Deliveries.Add(delivery);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Bell notification saved. DeliveryID={Id} Key={Key}",
                delivery.DeliveryId, request.IdempotencyKey);
        }
        catch (Exception ex)
        {
            _unitOfWork.Detach(delivery);
            _logger.LogWarning(ex,
                "Could not save bell notification (possible duplicate). Key={Key}",
                request.IdempotencyKey);
            return;
        }

        var unreadCount = await _unitOfWork.Deliveries.CountAsync(
            request.AccountId,
            NotificationChannels.WebBell,
            NotificationStatuses.Unread,
            ct);

        _ = _hub.PushToUserAsync(request.AccountId, new BellNotificationDto
        {
            DeliveryId       = delivery.DeliveryId,
            NotificationType = request.NotificationType,
            Title            = request.Title,
            Message          = request.Message,
            ImageUrl         = request.ImageUrl,
            ActionType       = request.ActionType,
            ActionTarget     = request.ActionTarget,
            CreatedAt        = delivery.CreatedAt,
            UnreadCount      = unreadCount,
        }, ct);
    }
}
