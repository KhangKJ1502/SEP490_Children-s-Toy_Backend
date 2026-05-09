using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Features.Notifications.Handlers;

/// <summary>
/// Handles shipping webhook events (GHN) pushed via DomainEventOutbox.
/// ORDER notifications bypass preference check (spec §8 rule 6).
/// </summary>
public class ShippingWebhookHandler : IOutboxEventHandler
{
    public string EventType => "shipping.status_changed";

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public ShippingWebhookHandler(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var orderId        = root.GetProperty("orderId").GetInt32();
        var providerStatus = root.TryGetProperty("providerStatus", out var ps) ? ps.GetString() : "";

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        // Map provider status to event type
        var notificationEventType = MapToEventType(providerStatus ?? "");
        if (notificationEventType is null) return;

        var (title, message, templateCode, sendEmail) = notificationEventType switch
        {
            var t when t == NotificationEventTypes.OrderDelivering     => ("Out for delivery", $"Your order {order.OrderCode} is out for delivery. Please keep your phone nearby.", NotificationTemplates.OrderShipping, true),
            var t when t == NotificationEventTypes.OrderDelivered      => ("Delivered successfully", $"Your order {order.OrderCode} has been delivered. Enjoy your purchase!", NotificationTemplates.OrderDelivered, true),
            var t when t == NotificationEventTypes.OrderDeliveryFailed => ("Delivery failed", $"Delivery for order {order.OrderCode} was unsuccessful. The carrier will retry.", NotificationTemplates.OrderDeliveryFailed, true),
            var t when t == NotificationEventTypes.OrderReturning      => ("Order being returned", $"Your order {order.OrderCode} is being returned to our warehouse.", (string?)null, false),
            var t when t == NotificationEventTypes.OrderReturnCompleted => ("Return completed", $"Your order {order.OrderCode} has been returned. Refund will be processed shortly.", (string?)null, true),
            _ => (null, null, null, false)
        };

        if (title is null) return;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            Title              = title,
            Message            = message!,
            SendBell           = true,
            SendEmail          = sendEmail,
            TemplateCode       = templateCode,
            ActionTarget       = $"/orders/{orderId}",
            IdempotencyKey     = $"{notificationEventType}:{orderId}:{order.AccountId}",
        }, ct);
    }

    private static string? MapToEventType(string providerStatus) =>
        providerStatus.ToLowerInvariant() switch
        {
            "delivering"     => NotificationEventTypes.OrderDelivering,
            "delivered"      => NotificationEventTypes.OrderDelivered,
            "delivery_failed"=> NotificationEventTypes.OrderDeliveryFailed,
            "return"         => NotificationEventTypes.OrderReturning,
            "returned"       => NotificationEventTypes.OrderReturnCompleted,
            _                => null,
        };
}

/// <summary>
/// Handles specific shipping webhook events now published with distinct event types.
/// </summary>
public class OrderDeliveringHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.OrderDelivering;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public OrderDeliveringHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;
        var orderId = root.GetProperty("orderId").GetInt32();

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            Title              = "Out for delivery",
            Message            = $"Your order {order.OrderCode} is out for delivery. Please keep your phone nearby.",
            SendBell           = true,
            SendEmail          = true,
            TemplateCode       = NotificationTemplates.OrderShipping,
            ActionTarget       = $"/orders/{orderId}",
            IdempotencyKey     = $"{EventType}:{orderId}:{order.AccountId}",
        }, ct);
    }
}

public class OrderDeliveredHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.OrderDelivered;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public OrderDeliveredHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;
        var orderId = root.GetProperty("orderId").GetInt32();

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            Title              = "Delivered successfully",
            Message            = $"Your order {order.OrderCode} has been delivered. Please leave a review!",
            SendBell           = true,
            SendEmail          = true,
            TemplateCode       = NotificationTemplates.OrderDelivered,
            ActionTarget       = $"/orders/{orderId}/review",
            IdempotencyKey     = $"{EventType}:{orderId}:{order.AccountId}",
        }, ct);
    }
}

public class MerchPickedUpHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.MerchPickedUp;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public MerchPickedUpHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;
        var orderId = root.GetProperty("orderId").GetInt32();

        var merch = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 4 }, ct);

        foreach (var m in merch)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = m.AccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                Title              = "Carrier picked up order",
                Message            = $"Order #{orderId} has been picked up by the carrier.",
                SendBell           = true,
                SendEmail          = false,
                ActionTarget       = $"/admin/orders/{orderId}",
                IdempotencyKey     = $"{EventType}:{orderId}:{m.AccountId}",
            }, ct);
        }
    }
}

public class MerchReturnedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.MerchReturned;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public MerchReturnedHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;
        var orderId = root.GetProperty("orderId").GetInt32();

        var merch = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 4 }, ct);

        foreach (var m in merch)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = m.AccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                Title              = "Return received",
                Message            = $"Order #{orderId} has been returned to the warehouse.",
                SendBell           = true,
                SendEmail          = false,
                ActionTarget       = $"/admin/orders/{orderId}",
                IdempotencyKey     = $"{EventType}:{orderId}:{m.AccountId}",
            }, ct);
        }
    }
}
