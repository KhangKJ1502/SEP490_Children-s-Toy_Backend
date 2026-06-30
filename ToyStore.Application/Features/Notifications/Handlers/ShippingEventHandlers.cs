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
        var refundId  = root.TryGetProperty("refundId", out var ri) ? ri.GetInt32() : 0;

        // Nếu có refundId → trỏ vào trang refund (System Return flow mới)
        var actionTarget = refundId > 0
            ? $"/admin/refunds/{refundId}"
            : $"/admin/orders/{orderId}";

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
                ActionTarget = actionTarget,
            }, ct);
        }
    }
}

public class StaffSystemRefundReadyHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.StaffSystemRefundReady;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public StaffSystemRefundReadyHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var refundId  = root.GetProperty("refundId").GetInt32();
        var orderId   = root.GetProperty("orderId").GetInt32();
        var orderCode = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";

        var staff = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 3 }, ct);
        foreach (var s in staff)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = s.AccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                TemplateCode       = NotificationTemplates.StaffSystemRefundReady,
                Placeholders       = new Dictionary<string, string>
                {
                    ["OrderCode"] = orderCode,
                },
                ReferenceId  = $"staff_refund_ready:{refundId}:{s.AccountId}",
                SendBell     = true,
                SendEmail    = false,
                ActionTarget = $"/admin/refunds/{refundId}",
            }, ct);
        }
    }
}

public class OrderDeliveryFailedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.OrderDeliveryFailed;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public OrderDeliveryFailedHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
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
            TemplateCode       = NotificationTemplates.OrderDeliveryFailed,
            Placeholders       = new Dictionary<string, string> { ["OrderCode"] = orderCode },
            ReferenceId        = $"{orderId}",
            SendBell           = true,
            SendEmail          = false,
            ActionTarget       = $"/profile/orders/{orderId}",
        }, ct);
    }
}

public class OrderReturnRefundPendingHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.OrderReturnRefundPending;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public OrderReturnRefundPendingHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
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
            TemplateCode       = NotificationTemplates.OrderReturnRefundPending,
            Placeholders       = new Dictionary<string, string> { ["OrderCode"] = orderCode },
            ReferenceId        = $"{orderId}",
            SendBell           = true,
            SendEmail          = true,
            ActionTarget       = $"/profile/orders/{orderId}",
        }, ct);
    }
}

public class OrderCancelledDeliveryFailHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.OrderCancelledDeliveryFail;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public OrderCancelledDeliveryFailHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
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
            TemplateCode       = NotificationTemplates.OrderCancelledDeliveryFail,
            Placeholders       = new Dictionary<string, string> { ["OrderCode"] = orderCode },
            ReferenceId        = $"{orderId}",
            SendBell           = true,
            SendEmail          = true,
            ActionTarget       = $"/profile/orders/{orderId}",
        }, ct);
    }
}

public class AdminReturnFailHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.OrderReturnFail;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public AdminReturnFailHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var orderId           = root.GetProperty("orderId").GetInt32();
        var orderCode         = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";
        var providerOrderCode = root.TryGetProperty("providerOrderCode", out var pc) ? pc.GetString() ?? "" : "";

        var admins = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 3 }, ct);
        foreach (var admin in admins)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = admin.AccountId,
                RecipientType      = RecipientTypes.Admin,
                NotificationType   = NotificationTypes.System,
                TemplateCode       = NotificationTemplates.AdminReturnFail,
                Placeholders       = new Dictionary<string, string>
                {
                    ["OrderCode"]         = orderCode,
                    ["ProviderOrderCode"] = providerOrderCode,
                },
                ReferenceId  = $"{orderId}:{admin.AccountId}",
                SendBell     = true,
                SendEmail    = true,
                ActionTarget = $"/admin/orders/{orderId}",
            }, ct);
        }
    }
}
