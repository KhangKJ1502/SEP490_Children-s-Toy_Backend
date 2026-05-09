using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Features.Notifications.Handlers;

/// <summary>
/// Notifies Merchandise team when an order is confirmed and ready to pack.
/// RecipientType uses "STAFF" as the DB CHECK constraint covers CUSTOMER|ADMIN|STAFF only.
/// </summary>
public class MerchReadyToPackHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.MerchReadyToPack;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public MerchReadyToPackHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var orderId   = root.GetProperty("orderId").GetInt32();
        var orderCode = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() : $"#{orderId}";

        // RoleId 4 = Merchandise (adjust if your DB differs)
        var merch = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 4 }, ct);

        foreach (var m in merch)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = m.AccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                Title              = "Order ready to pack",
                Message            = $"Order {orderCode} has been confirmed and is ready to pack.",
                SendBell           = true,
                SendEmail          = false,
                ActionTarget       = $"/admin/orders/{orderId}",
                TemplateCode       = NotificationTemplates.MerchReadyToPack,
                IdempotencyKey     = $"{EventType}:{orderId}:{m.AccountId}",
            }, ct);
        }
    }
}

/// <summary>
/// Handles the order.confirmed event for the customer-facing bell/email notification.
/// </summary>
public class OrderConfirmedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.OrderConfirmed;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public OrderConfirmedHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var orderId   = root.GetProperty("orderId").GetInt32();
        var orderCode = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() : $"#{orderId}";

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            Title              = "Order confirmed",
            Message            = $"Your order {orderCode} has been confirmed and is being prepared.",
            SendBell           = true,
            SendEmail          = false,
            ActionTarget       = $"/orders/{orderId}",
            TemplateCode       = NotificationTemplates.OrderConfirmed,
            IdempotencyKey     = $"{EventType}:{orderId}:{order.AccountId}",
        }, ct);
    }
}

/// <summary>
/// Handles the order.packing event for the customer-facing bell notification.
/// </summary>
public class OrderPackingHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.OrderPacking;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public OrderPackingHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var orderId   = root.GetProperty("orderId").GetInt32();
        var orderCode = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() : $"#{orderId}";

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            Title              = "Order is being packed",
            Message            = $"Your order {orderCode} is being packed and will be shipped soon.",
            SendBell           = true,
            SendEmail          = false,
            ActionTarget       = $"/orders/{orderId}",
            TemplateCode       = NotificationTemplates.OrderPacking,
            IdempotencyKey     = $"{EventType}:{orderId}:{order.AccountId}",
        }, ct);
    }
}

/// <summary>
/// Handles the order.shipped event for the customer-facing bell notification.
/// </summary>
public class OrderShippedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.OrderShipped;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public OrderShippedHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var orderId        = root.GetProperty("orderId").GetInt32();
        var orderCode      = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() : $"#{orderId}";
        var trackingNumber = root.TryGetProperty("trackingNumber", out var tn) ? tn.GetString() : "";

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            Title              = "Order shipped",
            Message            = $"Your order {orderCode} has been handed to the carrier. Tracking: {trackingNumber}",
            SendBell           = true,
            SendEmail          = false,
            TemplateCode       = NotificationTemplates.OrderShipping,
            ActionTarget       = $"/orders/{orderId}",
            IdempotencyKey     = $"{EventType}:{orderId}:{order.AccountId}",
        }, ct);
    }
}

/// <summary>
/// Handles the order.cancelled event for customer notification.
/// </summary>
public class OrderCancelledHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.OrderCancelled;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public OrderCancelledHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var orderId   = root.GetProperty("orderId").GetInt32();
        var orderCode = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() : $"#{orderId}";
        var reason    = root.TryGetProperty("reason", out var r) ? r.GetString() : "";

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            Title              = "Order cancelled",
            Message            = $"Your order {orderCode} has been cancelled. Reason: {reason}",
            SendBell           = true,
            SendEmail          = true,
            TemplateCode       = NotificationTemplates.OrderCancelled,
            ActionTarget       = $"/orders/{orderId}",
            IdempotencyKey     = $"{EventType}:{orderId}:{order.AccountId}",
        }, ct);
    }
}
