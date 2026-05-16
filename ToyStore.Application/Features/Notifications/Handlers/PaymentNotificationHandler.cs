using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Features.Notifications.Handlers;

public class PaymentSuccessHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.PaymentSuccess;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public PaymentSuccessHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var accountId       = root.GetProperty("accountId").GetInt32();
        var orderId         = root.GetProperty("orderId").GetInt32();
        var transactionCode = root.TryGetProperty("transactionCode", out var tc) ? tc.GetString() ?? "" : "";
        var amount          = root.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0;
        var orderCode       = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.PaymentSuccess,
            Placeholders       = new Dictionary<string, string>
            {
                ["Amount"]    = $"{amount:N0}",
                ["OrderCode"] = orderCode,
            },
            ReferenceId  = $"{orderId}",
            SendBell     = true,
            SendEmail    = true,
            ActionTarget = $"/profile/orders/{orderId}",
        }, ct);
    }
}

public class PaymentFailedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.PaymentFailed;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public PaymentFailedHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var accountId = root.GetProperty("accountId").GetInt32();
        var orderId   = root.GetProperty("orderId").GetInt32();
        var orderCode = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";
        var amount    = root.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.PaymentFailed,
            Placeholders       = new Dictionary<string, string>
            {
                ["Amount"]    = $"{amount:N0}",
                ["OrderCode"] = orderCode,
            },
            ReferenceId  = $"{orderId}",
            SendBell     = true,
            SendEmail    = true,
            ActionTarget = $"/profile/orders/{orderId}/payment",
        }, ct);
    }
}

public class WalletTopupHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.WalletTopup;
    private readonly INotificationDispatcher _dispatcher;

    public WalletTopupHandler(INotificationDispatcher dispatcher) => _dispatcher = dispatcher;

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var accountId    = root.GetProperty("accountId").GetInt32();
        var amount       = root.TryGetProperty("amount", out var a) ? a.GetDecimal() : 0;
        var balanceAfter = root.TryGetProperty("balanceAfter", out var b) ? b.GetDecimal() : 0;
        var txnId        = root.TryGetProperty("walletTransactionId", out var t) ? t.GetInt32() : 0;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.WalletTopup,
            Placeholders       = new Dictionary<string, string>
            {
                ["Amount"]  = $"{amount:N0} VND",
                ["Balance"] = $"{balanceAfter:N0} VND",
            },
            ReferenceId  = $"{txnId}",
            SendBell     = true,
            SendEmail    = true,
            ActionTarget = "/wallet",
        }, ct);
    }
}

public class WalletRefundHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.WalletRefund;
    private readonly INotificationDispatcher _dispatcher;

    public WalletRefundHandler(INotificationDispatcher dispatcher) => _dispatcher = dispatcher;

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var accountId   = root.GetProperty("accountId").GetInt32();
        var amount      = root.TryGetProperty("amount", out var a) ? a.GetDecimal() : 0;
        var walletTxnId = root.TryGetProperty("walletTransactionId", out var t) ? t.GetInt32() : 0;
        var orderCode   = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? "" : "";

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.WalletRefund,
            Placeholders       = new Dictionary<string, string>
            {
                ["Amount"]    = $"{amount:N0}",
                ["OrderCode"] = orderCode,
            },
            ReferenceId  = $"{walletTxnId}",
            SendBell     = true,
            SendEmail    = true,
            ActionTarget = "/wallet",
        }, ct);
    }
}
