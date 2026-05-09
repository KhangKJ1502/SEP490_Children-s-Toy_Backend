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
    private readonly IUserPreferenceChecker _prefChecker;

    public OrderPlacedHandler(
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

        var orderId    = root.GetProperty("orderId").GetInt32();
        var orderCode  = root.GetProperty("orderCode").GetString() ?? "";
        var totalAmount = root.TryGetProperty("totalAmount", out var ta) ? ta.GetDecimal() : 0;

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        // Customer bell + email
        if (await _prefChecker.CanSendAsync(order.AccountId, PreferenceKeys.OrderUpdates, ct))
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = order.AccountId,
                RecipientType      = RecipientTypes.Customer,
                NotificationType   = NotificationTypes.Order,
                Title              = "Đặt hàng thành công",
                Message            = $"Đơn hàng {orderCode} ({totalAmount:N0}₫) đã được đặt thành công",
                SendBell           = true,
                SendEmail          = true,
                TemplateCode       = NotificationTemplates.OrderPlaced,
                ActionTarget       = $"/orders/{orderId}",
                IdempotencyKey     = $"{EventType}:{orderId}:{order.AccountId}",
                Payload            = new Dictionary<string, object>
                {
                    ["orderId"]    = orderId,
                    ["orderCode"]  = orderCode,
                    ["totalAmount"] = totalAmount,
                },
            }, ct);
        }

        // Staff bell — new pending order
        var staffAccounts = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct); // Assuming RoleId 2 is Staff

        foreach (var staff in staffAccounts)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staff.AccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                Title              = "Đơn hàng mới",
                Message            = $"Đơn {orderCode} vừa được đặt",
                SendBell           = true,
                SendEmail          = false,
                TemplateCode       = NotificationTemplates.StaffNewOrder,
                ActionTarget       = $"/admin/orders/{orderId}",
                IdempotencyKey     = $"order.new_pending:{orderId}:{staff.AccountId}",
            }, ct);
        }
    }
}
