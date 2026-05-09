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
        var transactionCode = root.TryGetProperty("transactionCode", out var tc) ? tc.GetString() : "";
        var amount          = root.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            Title              = "Payment successful",
            Message            = $"Payment of {amount:N0}₫ for your order was successful. Txn: {transactionCode}",
            SendBell           = true,
            SendEmail          = true,
            TemplateCode       = NotificationTemplates.PaymentSuccess,
            ActionTarget       = $"/orders/{orderId}",
            IdempotencyKey     = $"{EventType}:{orderId}:{accountId}",
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

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            Title              = "Payment failed",
            Message            = "Your payment was not successful. Please try again.",
            SendBell           = true,
            SendEmail          = true,
            TemplateCode       = NotificationTemplates.PaymentFailed,
            ActionTarget       = $"/orders/{orderId}/payment",
            IdempotencyKey     = $"{EventType}:{orderId}:{accountId}",
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
            Title              = "Wallet top-up successful",
            Message            = $"Your wallet has been topped up with {amount:N0}₫. New balance: {balanceAfter:N0}₫",
            SendBell           = true,
            SendEmail          = true,
            TemplateCode       = NotificationTemplates.WalletTopup,
            ActionTarget       = "/wallet",
            IdempotencyKey     = $"{EventType}:{txnId}:{accountId}",
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

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            Title              = "Refund processed",
            Message            = $"{amount:N0}₫ has been refunded to your wallet.",
            SendBell           = true,
            SendEmail          = true,
            TemplateCode       = NotificationTemplates.WalletRefund,
            ActionTarget       = "/wallet",
            IdempotencyKey     = $"{EventType}:{walletTxnId}:{accountId}",
        }, ct);
    }
}
