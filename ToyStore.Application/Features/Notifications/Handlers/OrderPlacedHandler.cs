using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Application.Features.Notifications.Handlers;

public class OrderPlacedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.OrderPlaced;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IShiftAssignmentService _assignmentService;

    public OrderPlacedHandler(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher,
        IShiftAssignmentService assignmentService)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _assignmentService = assignmentService;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var orderId     = root.GetProperty("orderId").GetInt32();
        var orderCode   = root.GetProperty("orderCode").GetString() ?? "";
        var totalAmount = root.TryGetProperty("totalAmount", out var ta) ? ta.GetDecimal() : 0;

        await _assignmentService.AutoAssignOrderAsync(orderId, ct);

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is null) return;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = order.AccountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.OrderPlaced,
            Placeholders       = new Dictionary<string, string>
            {
                ["OrderCode"]   = orderCode,
                ["TotalAmount"] = $"{totalAmount:N0}",
            },
            ReferenceId  = $"{orderId}",
            SendBell     = true,
            SendEmail    = true,
            ActionTarget = $"/profile/orders/{orderId}",
            Payload      = new Dictionary<string, object>
            {
                ["orderId"]     = orderId,
                ["orderCode"]   = orderCode,
                ["totalAmount"] = totalAmount,
            },
        }, ct);

        var staffAccounts = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);
        foreach (var staff in staffAccounts)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staff.AccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                TemplateCode       = NotificationTemplates.StaffNewOrder,
                Placeholders       = new Dictionary<string, string>
                {
                    ["OrderCode"]   = orderCode,
                    ["TotalAmount"] = $"{totalAmount:N0}",
                },
                ReferenceId  = $"{orderId}:{staff.AccountId}",
                SendBell     = true,
                SendEmail    = false,
                ActionTarget = $"/admin/orders/{orderId}",
            }, ct);
        }
    }
}
