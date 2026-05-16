using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Application.Features.Notifications.Handlers;

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

        var orderId   = root.GetProperty("orderId").GetInt32();
        var orderCode = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";
        var shipper   = root.TryGetProperty("shipperName", out var sn) ? sn.GetString() ?? "" : "";

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
                ["OrderCode"]   = orderCode,
                ["ShipperName"] = shipper,
            },
            ReferenceId  = $"{orderId}",
            SendBell     = true,
            SendEmail    = false,
            ActionTarget = $"/profile/orders/{orderId}",
        }, ct);
    }
}

/// <summary>
/// Ví dụ sử dụng hệ thống template mới — đơn hàng giao thành công.
/// Dispatcher sẽ: lookup template ORDER_DELIVERED → render {{OrderCode}} → kiểm tra
/// preferences → idempotency key ORDER_DELIVERED:{accountId}:{orderId}:WEB_BELL/EMAIL → gửi.
/// </summary>
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

        var orderId   = root.GetProperty("orderId").GetInt32();
        var orderCode = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.OrderDelivered,
            Placeholders       = new Dictionary<string, string>
            {
                ["OrderCode"] = orderCode,
            },
            ReferenceId  = $"{orderId}",
            SendBell     = true,
            SendEmail    = true,
            ActionTarget = $"/profile/orders/{orderId}",
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
                TemplateCode       = NotificationTemplates.MerchPickedUp,
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
                TemplateCode       = NotificationTemplates.MerchReturned,
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
