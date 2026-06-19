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
        var delivery = new Delivery
        {
            AccountId        = request.AccountId,
            RecipientType    = request.RecipientType,
            Channel          = NotificationChannels.WebBell,
            NotificationType = request.NotificationType,
            Title            = request.Title,
            Message          = request.Message,
            Payload          = request.PayloadJson ?? "{}",
            Status           = NotificationStatuses.Unread,
            TemplateCode     = request.TemplateCode,
            ImageUrl         = request.ImageUrl,
            ActionType       = request.ActionType,
            ActionTarget     = request.ActionTarget,
            IdempotencyKey   = request.IdempotencyKey,
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
