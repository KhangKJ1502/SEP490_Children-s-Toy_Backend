using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Application.Features.Notifications.Handlers;

public class OrderReadyForAssignmentHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.OrderConfirmed;

    private readonly IShiftAssignmentService _assignmentService;

    public OrderReadyForAssignmentHandler(IShiftAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var orderId = root.GetProperty("orderId").GetInt32();
        await _assignmentService.AutoAssignOrderAsync(orderId, ct);
    }
}

public class OrderAutoAssignedHandler : IOutboxEventHandler
{
    public string EventType => ShiftEventTypes.OrderAssigned;

    private readonly INotificationDispatcher _dispatcher;

    public OrderAutoAssignedHandler(INotificationDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var orderId        = root.GetProperty("orderId").GetInt32();
        var orderCode      = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";
        var staffAccountId = root.TryGetProperty("staffAccountId", out var s) ? s.GetInt32() : 0;
        var merchAccountId = root.TryGetProperty("merchAccountId", out var m) ? m.GetInt32() : 0;

        if (staffAccountId > 0)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staffAccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                TemplateCode       = NotificationTemplates.OrderAssigned,
                Placeholders       = new Dictionary<string, string>
                {
                    ["OrderCode"] = orderCode,
                },
                ReferenceId  = $"{orderId}:{staffAccountId}",
                SendBell     = true,
                SendEmail    = false,
                ActionTarget = $"/admin/orders/{orderId}",
            }, ct);
        }

        if (merchAccountId > 0)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = merchAccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                TemplateCode       = NotificationTemplates.OrderAssigned,
                Placeholders       = new Dictionary<string, string>
                {
                    ["OrderCode"] = orderCode,
                },
                ReferenceId  = $"{orderId}:{merchAccountId}",
                SendBell     = true,
                SendEmail    = false,
                ActionTarget = $"/admin/orders/{orderId}",
            }, ct);
        }
    }
}

public class OrderQueuedHandler : IOutboxEventHandler
{
    public string EventType => ShiftEventTypes.OrderQueued;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public OrderQueuedHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
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
        var reason    = root.TryGetProperty("reason", out var r) ? r.GetString() ?? "UNKNOWN" : "UNKNOWN";

        var admins = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);
        foreach (var admin in admins)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = admin.AccountId,
                RecipientType      = RecipientTypes.Admin,
                NotificationType   = NotificationTypes.System,
                TemplateCode       = NotificationTemplates.AdminOrderQueued,
                Placeholders       = new Dictionary<string, string>
                {
                    ["OrderCode"] = orderCode,
                    ["Reason"]    = reason,
                },
                ReferenceId  = $"{orderId}:{admin.AccountId}",
                SendBell     = true,
                SendEmail    = true,
                ActionTarget = $"/admin/orders/{orderId}",
            }, ct);
        }
    }
}

public class CapacityFreedHandler : IOutboxEventHandler
{
    public string EventType => ShiftEventTypes.CapacityFreed;

    private readonly IShiftAssignmentService _assignmentService;

    public CapacityFreedHandler(IShiftAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        await _assignmentService.TryAssignOldestQueueAsync(ct);
    }
}

public class ShiftStartedHandler : IOutboxEventHandler
{
    public string EventType => ShiftEventTypes.ShiftStarted;

    private readonly INotificationDispatcher _dispatcher;

    public ShiftStartedHandler(INotificationDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var scheduleId = root.GetProperty("scheduleId").GetInt32();
        var accountId  = root.GetProperty("accountId").GetInt32();
        var shiftName  = root.TryGetProperty("shiftName", out var s) ? s.GetString() ?? "Work shift" : "Work shift";

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Staff,
            NotificationType   = NotificationTypes.System,
            TemplateCode       = NotificationTemplates.StaffShiftStarted,
            Placeholders       = new Dictionary<string, string>
            {
                ["ShiftName"] = shiftName,
            },
            ReferenceId  = $"{scheduleId}:{accountId}",
            SendBell     = true,
            SendEmail    = false,
            ActionTarget = $"/admin/shifts/{scheduleId}",
        }, ct);
    }
}

public class ShiftEndedWithPendingOrdersHandler : IOutboxEventHandler
{
    public string EventType => ShiftEventTypes.ShiftEndedWithPendingOrders;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public ShiftEndedWithPendingOrdersHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var scheduleId   = root.GetProperty("scheduleId").GetInt32();
        var accountId    = root.GetProperty("accountId").GetInt32();
        var shiftName    = root.TryGetProperty("shiftName", out var s) ? s.GetString() ?? "Work shift" : "Work shift";
        var currentLoad  = root.TryGetProperty("currentLoad", out var c) ? c.GetInt32() : 0;

        var admins = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);
        foreach (var admin in admins)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = admin.AccountId,
                RecipientType      = RecipientTypes.Admin,
                NotificationType   = NotificationTypes.System,
                TemplateCode       = NotificationTemplates.AdminShiftEndedPending,
                Placeholders       = new Dictionary<string, string>
                {
                    ["ShiftName"]   = shiftName,
                    ["AccountId"]   = accountId.ToString(),
                    ["CurrentLoad"] = currentLoad.ToString(),
                },
                ReferenceId  = $"{scheduleId}:{admin.AccountId}",
                SendBell     = true,
                SendEmail    = true,
                ActionTarget = $"/admin/shifts/{scheduleId}",
            }, ct);
        }
    }
}
