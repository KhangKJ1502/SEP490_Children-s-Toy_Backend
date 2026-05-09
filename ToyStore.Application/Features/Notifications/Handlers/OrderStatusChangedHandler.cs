using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Features.Notifications.Handlers;

/// <summary>
/// Handles manual staff-triggered order status transitions:
/// confirmed, packing, shipped, cancelled.
/// </summary>
public class OrderStatusChangedHandler : IOutboxEventHandler
{
    public string EventType => "order.status_changed";

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IUserPreferenceChecker _prefChecker;

    public OrderStatusChangedHandler(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher,
        IUserPreferenceChecker prefChecker)
    {
        _unitOfWork  = unitOfWork;
        _dispatcher  = dispatcher;
        _prefChecker = prefChecker;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var orderId   = root.GetProperty("orderId").GetInt32();
        var newStatus = root.GetProperty("newStatus").GetByte();

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        if (!await _prefChecker.CanSendAsync(order.AccountId, PreferenceKeys.OrderUpdates, ct))
            return;

        var (title, message, templateCode, eventType, sendEmail) = newStatus switch
        {
            2 => ("Đơn hàng đã xác nhận",   $"Đơn {order.OrderCode} đã được xác nhận",      NotificationTemplates.OrderConfirmed, NotificationEventTypes.OrderConfirmed, false),
            3 => ("Đang đóng gói",           $"Đơn {order.OrderCode} đang được đóng gói",    NotificationTemplates.OrderPacking,   NotificationEventTypes.OrderPacking,   false),
            4 => ("Đã bàn giao vận chuyển",  $"Đơn {order.OrderCode} đã được chuyển đi",    NotificationTemplates.OrderShipping,  NotificationEventTypes.OrderShipped,   false),
            8 => ("Đơn hàng đã huỷ",         $"Đơn {order.OrderCode} đã bị huỷ. Lý do: {order.CancelReason}", NotificationTemplates.OrderCancelled, NotificationEventTypes.OrderCancelled, true),
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
