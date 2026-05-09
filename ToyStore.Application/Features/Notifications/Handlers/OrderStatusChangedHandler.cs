using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Features.Notifications.Handlers;

/// <summary>
/// Handles per-status order events published by AdminOrderService.
/// Each event type dispatches a targeted notification to the customer.
/// ORDER notifications bypass user preference checks (spec §8 rule 6).
/// </summary>
public class OrderStatusChangedHandler : IOutboxEventHandler
{
    public string EventType => "order.status_changed";

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public OrderStatusChangedHandler(
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

        var orderId   = root.GetProperty("orderId").GetInt32();
        var newStatus = root.GetProperty("newStatus").GetByte();

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        // No preference gating for ORDER type — always send
        var (title, message, templateCode, eventType, sendEmail) = newStatus switch
        {
            2 => ("Order confirmed",          $"Your order {order.OrderCode} has been confirmed and is being prepared.", NotificationTemplates.OrderConfirmed, NotificationEventTypes.OrderConfirmed, false),
            3 => ("Order is being packed",    $"Your order {order.OrderCode} is being packed and will be shipped soon.", NotificationTemplates.OrderPacking,   NotificationEventTypes.OrderPacking,   false),
            4 => ("Order shipped",            $"Your order {order.OrderCode} has been handed to the carrier.",           NotificationTemplates.OrderShipping,  NotificationEventTypes.OrderShipped,   false),
            8 => ("Order cancelled",          $"Your order {order.OrderCode} has been cancelled. Reason: {order.CancelReason}", NotificationTemplates.OrderCancelled, NotificationEventTypes.OrderCancelled, true),
            _ => (null, null, null, null, false),
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
            IdempotencyKey     = $"{eventType}:{orderId}:{order.AccountId}",
        }, ct);
    }
}
