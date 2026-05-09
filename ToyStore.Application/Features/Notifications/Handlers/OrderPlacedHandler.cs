using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Features.Notifications.Handlers;

public class OrderPlacedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.OrderPlaced;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public OrderPlacedHandler(
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

        var orderId     = root.GetProperty("orderId").GetInt32();
        var orderCode   = root.GetProperty("orderCode").GetString() ?? "";
        var totalAmount = root.TryGetProperty("totalAmount", out var ta) ? ta.GetDecimal() : 0;

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        // Customer bell + email — ORDER notifications bypass preference check (spec §8 rule 6)
        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            Title              = "Order placed successfully",
            Message            = $"Your order {orderCode} ({totalAmount:N0}₫) has been placed. We will process it shortly.",
            SendBell           = true,
            SendEmail          = true,
            TemplateCode       = NotificationTemplates.OrderPlaced,
            ActionTarget       = $"/orders/{orderId}",
            IdempotencyKey     = $"{EventType}:{orderId}:{order.AccountId}",
            Payload            = new Dictionary<string, object>
            {
                ["orderId"]     = orderId,
                ["orderCode"]   = orderCode,
                ["totalAmount"] = totalAmount,
            },
        }, ct);

        // Staff bell — new pending order
        var staffAccounts = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);

        foreach (var staff in staffAccounts)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staff.AccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                Title              = "New order received",
                Message            = $"Order {orderCode} has just been placed and is waiting for confirmation.",
                SendBell           = true,
                SendEmail          = false,
                TemplateCode       = NotificationTemplates.StaffNewOrder,
                ActionTarget       = $"/admin/orders/{orderId}",
                IdempotencyKey     = $"order.new_pending:{orderId}:{staff.AccountId}",
            }, ct);
        }
    }
}
