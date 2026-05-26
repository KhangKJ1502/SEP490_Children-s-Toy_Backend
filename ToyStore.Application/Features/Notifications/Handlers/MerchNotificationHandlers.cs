using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Features.Notifications.Handlers;

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
        var orderCode = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";

        var merch = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 4 }, ct);
        foreach (var m in merch)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = m.AccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                TemplateCode       = NotificationTemplates.MerchReadyToPack,
                Placeholders       = new Dictionary<string, string>
                {
                    ["OrderCode"] = orderCode,
                },
                ReferenceId  = $"{orderId}:{m.AccountId}",
                SendBell     = true,
                SendEmail    = false,
                ActionTarget = $"/admin/orders/{orderId}",
            }, ct);
        }
    }
}

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
        var orderCode = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.OrderConfirmed,
            Placeholders       = new Dictionary<string, string>
            {
                ["OrderCode"] = orderCode,
            },
            ReferenceId  = $"{orderId}",
            SendBell     = true,
            SendEmail    = false,
            ActionTarget = $"/profile/orders/{orderId}",
        }, ct);
    }
}

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
        var orderCode = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.OrderPacking,
            Placeholders       = new Dictionary<string, string>
            {
                ["OrderCode"] = orderCode,
            },
            ReferenceId  = $"{orderId}",
            SendBell     = true,
            SendEmail    = false,
            ActionTarget = $"/profile/orders/{orderId}",
        }, ct);
    }
}

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
        var orderCode      = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";
        var trackingNumber = root.TryGetProperty("trackingNumber", out var tn) ? tn.GetString() ?? "" : "";

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.OrderShipping,
            Placeholders       = new Dictionary<string, string>
            {
                ["OrderCode"]       = orderCode,
                ["ShipperName"]     = trackingNumber,
            },
            ReferenceId  = $"{orderId}",
            SendBell     = true,
            SendEmail    = true,
            ActionTarget = $"/profile/orders/{orderId}",
            Payload      = new Dictionary<string, object>
            {
                ["orderId"]        = orderId,
                ["orderCode"]      = orderCode,
                ["trackingNumber"] = trackingNumber
            }
        }, ct);
    }
}

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
        var orderCode = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";
        var reason    = root.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "";

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        bool cancelledByCustomer = order.CancelledBy.HasValue && order.CancelledBy.Value == order.AccountId;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.OrderCancelled,
            Placeholders       = new Dictionary<string, string>
            {
                ["OrderCode"]    = orderCode,
                ["CancelReason"] = reason,
            },
            ReferenceId  = $"{orderId}",
            SendBell     = !cancelledByCustomer,
            SendEmail    = !cancelledByCustomer,
            ActionTarget = $"/profile/orders/{orderId}",
        }, ct);
    }
}
