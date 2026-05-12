using System.Text.Json;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Services.Notifications;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationHubService _hub;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IUnitOfWork unitOfWork,
        INotificationHubService hub,
        ILogger<NotificationDispatcher> logger)
    {
        _unitOfWork = unitOfWork;
        _hub        = hub;
        _logger     = logger;
    }

    public async Task DispatchAsync(NotificationContext context, CancellationToken ct = default)
    {
        if (context.SendBell)
            await DispatchBellAsync(context, ct);

        if (context.SendEmail)
            await DispatchEmailAsync(context, ct);
    }

    public async Task DispatchBulkAsync(IEnumerable<NotificationContext> contexts, CancellationToken ct = default)
    {
        foreach (var ctx in contexts)
            await DispatchAsync(ctx, ct);
    }

    private async Task DispatchBellAsync(NotificationContext context, CancellationToken ct)
    {
        var bellKey = context.IdempotencyKey is not null
            ? $"{context.IdempotencyKey}:{NotificationChannels.WebBell}"
            : null;

        if (bellKey is not null && await _unitOfWork.Deliveries.ExistsByIdempotencyKeyAsync(bellKey, ct))
        {
            _logger.LogInformation("Bell notification already dispatched. IdempotencyKey={Key}", bellKey);
            return;
        }

        var delivery = new Delivery
        {
            AccountId        = context.RecipientAccountId,
            RecipientType    = context.RecipientType,
            Channel          = NotificationChannels.WebBell,
            NotificationType = context.NotificationType,
            Title            = context.Title,
            Message          = context.Message,
            Payload          = context.Payload is not null
                               ? JsonSerializer.Serialize(context.Payload)
                               : "{}",
            Status           = NotificationStatuses.Unread,
            TemplateCode     = context.TemplateCode,
            ImageUrl         = context.ImageUrl,
            ActionType       = context.ActionType,
            ActionTarget     = context.ActionTarget,
            IdempotencyKey   = bellKey,
            CampaignId       = context.CampaignId,
            CreatedAt        = DateTime.Now,
        };

        try
        {
            _unitOfWork.Deliveries.Add(delivery);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Bell notification dispatched. DeliveryID={DeliveryId} IdempotencyKey={Key}",
                delivery.DeliveryId, bellKey);
        }
        catch (Exception ex)
        {
            _unitOfWork.Detach(delivery);
            // Note: In a real Clean Architecture, we'd have a specific exception for unique constraints
            // from the repository layer. For now, we log and skip to maintain idempotency behavior.
            _logger.LogWarning(ex, "Could not dispatch bell notification. It might be a duplicate. IdempotencyKey={Key}", bellKey);
            return;
        }

        var unreadCount = await _unitOfWork.Deliveries.CountAsync(
            context.RecipientAccountId, 
            NotificationChannels.WebBell, 
            NotificationStatuses.Unread, ct);

        _ = _hub.PushToUserAsync(context.RecipientAccountId, new BellNotificationDto
        {
            DeliveryId       = delivery.DeliveryId,
            NotificationType = context.NotificationType,
            Title            = context.Title,
            Message          = context.Message,
            ImageUrl         = context.ImageUrl,
            ActionType       = context.ActionType,
            ActionTarget     = context.ActionTarget,
            CreatedAt        = delivery.CreatedAt,
            UnreadCount      = unreadCount,
        }, ct);
    }

    private async Task DispatchEmailAsync(NotificationContext context, CancellationToken ct)
    {
        var emailKey = context.IdempotencyKey is not null
            ? $"{context.IdempotencyKey}:{NotificationChannels.Email}"
            : null;

        if (emailKey is not null && await _unitOfWork.Deliveries.ExistsByIdempotencyKeyAsync(emailKey, ct))
        {
            _logger.LogInformation("Email notification already queued. IdempotencyKey={Key}", emailKey);
            return;
        }

        var emailDelivery = new Delivery
        {
            AccountId        = context.RecipientAccountId,
            RecipientType    = context.RecipientType,
            Channel          = NotificationChannels.Email,
            NotificationType = context.NotificationType,
            Title            = context.Title,
            Message          = context.Message,
            Payload          = context.Payload is not null
                               ? JsonSerializer.Serialize(context.Payload)
                               : "{}",
            Status           = NotificationStatuses.Unread,
            EmailStatus      = EmailStatuses.Pending,
            TemplateCode     = context.TemplateCode,
            ImageUrl         = context.ImageUrl,
            ActionType       = context.ActionType,
            ActionTarget     = context.ActionTarget,
            IdempotencyKey   = emailKey,
            CampaignId       = context.CampaignId,
            CreatedAt        = DateTime.Now,
        };

        try
        {
            _unitOfWork.Deliveries.Add(emailDelivery);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Email notification queued. DeliveryID={DeliveryId} IdempotencyKey={Key}",
                emailDelivery.DeliveryId, emailKey);
        }
        catch (Exception ex)
        {
            _unitOfWork.Detach(emailDelivery);
            _logger.LogWarning(ex, "Could not queue email notification. It might be a duplicate. IdempotencyKey={Key}", emailKey);
        }
    }
}
