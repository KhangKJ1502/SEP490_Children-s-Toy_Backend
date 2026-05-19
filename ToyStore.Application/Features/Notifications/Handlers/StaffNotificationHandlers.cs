using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Features.Notifications.Handlers;

public class RefundRequestHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.RefundNewRequest;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public RefundRequestHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var refundId     = root.GetProperty("refundId").GetInt32();
        var orderCode    = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? "" : "";
        var customerName = root.TryGetProperty("customerName", out var cn) ? cn.GetString() ?? "Customer" : "Customer";

        var staffs = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);

        foreach (var staff in staffs)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staff.AccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                TemplateCode       = NotificationTemplates.StaffRefundRequest,
                Placeholders       = new Dictionary<string, string>
                {
                    ["CustomerName"] = customerName,
                    ["OrderCode"]    = orderCode,
                },
                ReferenceId  = $"{refundId}:{staff.AccountId}",
                SendBell     = true,
                SendEmail    = false,
                ActionTarget = $"/admin/refunds/{refundId}",
            }, ct);
        }
    }
}

public class ReviewNeedsModerationHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.ReviewNeedsModeration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public ReviewNeedsModerationHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var reviewId    = root.GetProperty("reviewId").GetInt32();
        var productName = root.TryGetProperty("productName", out var pn) ? pn.GetString() ?? $"#{reviewId}" : $"#{reviewId}";
        var rating      = root.TryGetProperty("rating", out var r) ? r.GetInt32().ToString() : "?";

        var staffs = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);

        foreach (var staff in staffs)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staff.AccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.System,
                TemplateCode       = NotificationTemplates.StaffReviewModeration,
                Placeholders       = new Dictionary<string, string>
                {
                    ["ProductName"] = productName,
                    ["Rating"]      = rating,
                },
                ReferenceId  = $"{reviewId}:{staff.AccountId}",
                SendBell     = true,
                SendEmail    = false,
                ActionTarget = $"/admin/reviews/{reviewId}",
            }, ct);
        }
    }
}

public class ReviewLowRatingHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.ReviewLowRating;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public ReviewLowRatingHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var reviewId  = root.GetProperty("reviewId").GetInt32();
        var rating    = root.TryGetProperty("rating", out var r) ? r.GetInt32() : 0;
        var productId = root.TryGetProperty("productId", out var p) ? p.GetInt32() : 0;
        var productName = root.TryGetProperty("productName", out var pn) ? pn.GetString() ?? $"#{productId}" : $"#{productId}";

        var staffs = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);

        foreach (var staff in staffs)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staff.AccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.System,
                TemplateCode       = NotificationTemplates.StaffLowRating,
                Placeholders       = new Dictionary<string, string>
                {
                    ["ProductName"] = productName,
                    ["Rating"]      = rating.ToString(),
                },
                ReferenceId  = $"{reviewId}:{staff.AccountId}",
                SendBell     = true,
                SendEmail    = false,
                ActionTarget = $"/admin/reviews/{reviewId}",
            }, ct);
        }
    }
}

public class ReviewStaffRepliedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.ReviewStaffReplied;
    private readonly INotificationDispatcher _dispatcher;

    public ReviewStaffRepliedHandler(INotificationDispatcher dispatcher) => _dispatcher = dispatcher;

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var reviewId    = root.GetProperty("reviewId").GetInt32();
        var accountId   = root.GetProperty("accountId").GetInt32();
        var productName = root.TryGetProperty("productName", out var pn) ? pn.GetString() ?? "" : "";

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Blog,
            TemplateCode       = NotificationTemplates.ReviewStaffReplied,
            Placeholders       = new Dictionary<string, string>
            {
                ["ProductName"] = productName,
            },
            ReferenceId  = $"{reviewId}:{accountId}",
            SendBell     = true,
            SendEmail    = false,
            ActionTarget = $"/products/review/{reviewId}",
        }, ct);
    }
}

public class BlogCommentRepliedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.BlogCommentReplied;
    private readonly INotificationDispatcher _dispatcher;

    public BlogCommentRepliedHandler(INotificationDispatcher dispatcher) => _dispatcher = dispatcher;

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var accountId   = root.GetProperty("accountId").GetInt32();
        var replyBlogId = root.GetProperty("replyBlogId").GetInt32();
        var blogPostId  = root.TryGetProperty("blogPostId", out var b) ? b.GetInt32() : 0;
        var blogTitle   = root.TryGetProperty("blogTitle", out var bt) ? bt.GetString() ?? "" : "";

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Blog,
            TemplateCode       = NotificationTemplates.BlogCommentReplied,
            Placeholders       = new Dictionary<string, string>
            {
                ["BlogTitle"] = blogTitle,
            },
            ReferenceId  = $"{replyBlogId}:{accountId}",
            SendBell     = true,
            SendEmail    = false,
            ActionTarget = $"/blog/{blogPostId}#reply-{replyBlogId}",
        }, ct);
    }
}

public class StaffCancelRequestedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.StaffCancelRequested;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public StaffCancelRequestedHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var orderId      = root.GetProperty("orderId").GetInt32();

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order is not null && order.PaymentMethod == "SE_PAY" && order.PaymentStatus != "PAID")
        {
            return;
        }

        var orderCode    = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";
        var reason       = root.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "";
        var customerName = root.TryGetProperty("customerName", out var cn) ? cn.GetString() ?? "Customer" : "Customer";

        var staffs = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);

        foreach (var staff in staffs)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staff.AccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                TemplateCode       = NotificationTemplates.StaffCancelRequest,
                Placeholders       = new Dictionary<string, string>
                {
                    ["OrderCode"]    = orderCode,
                    ["CustomerName"] = customerName,
                    ["Reason"]       = reason,
                },
                ReferenceId  = $"{orderId}:{staff.AccountId}",
                SendBell     = true,
                SendEmail    = false,
                ActionTarget = $"/admin/orders/{orderId}",
            }, ct);
        }
    }
}

public class StaffOrderAssignedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.StaffOrderAssigned;
    private readonly INotificationDispatcher _dispatcher;

    public StaffOrderAssignedHandler(INotificationDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var orderId         = root.GetProperty("orderId").GetInt32();
        var orderCode       = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? $"#{orderId}" : $"#{orderId}";
        var targetAccountId = root.GetProperty("targetAccountId").GetInt32();

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = targetAccountId,
            RecipientType      = RecipientTypes.Staff,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.StaffOrderAssigned,
            Placeholders       = new Dictionary<string, string>
            {
                ["OrderCode"] = orderCode,
            },
            ReferenceId  = $"{orderId}:{targetAccountId}",
            SendBell     = true,
            SendEmail    = true,
            ActionTarget = $"/admin/orders/{orderId}",
        }, ct);
    }
}

public class RefundApprovedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.RefundApproved;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public RefundApprovedHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var refundId   = root.GetProperty("refundId").GetInt32();
        var orderCode  = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? "" : "";
        var customerId = root.GetProperty("customerId").GetInt32();
        var amount     = root.TryGetProperty("amount", out var am) ? am.GetDecimal() : 0;
        var orderId    = root.TryGetProperty("orderId", out var oi) ? oi.GetInt32() : 0;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = customerId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.RefundApproved,
            Placeholders       = new Dictionary<string, string>
            {
                ["OrderCode"] = orderCode,
                ["Amount"]    = $"{amount:N0}",
            },
            ReferenceId  = $"{refundId}",
            SendBell     = true,
            SendEmail    = true,
            ActionTarget = orderId > 0 ? $"/profile/orders/{orderId}" : "/refunds",
        }, ct);
    }
}

public class RefundRejectedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.RefundRejected;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public RefundRejectedHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var refundId   = root.GetProperty("refundId").GetInt32();
        var orderCode  = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? "" : "";
        var customerId = root.GetProperty("customerId").GetInt32();
        var orderId    = root.TryGetProperty("orderId", out var oi) ? oi.GetInt32() : 0;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = customerId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.RefundRejected,
            Placeholders       = new Dictionary<string, string>
            {
                ["OrderCode"] = orderCode,
            },
            ReferenceId  = $"{refundId}",
            SendBell     = true,
            SendEmail    = true,
            ActionTarget = orderId > 0 ? $"/profile/orders/{orderId}" : "/refunds",
        }, ct);
    }
}

public class RefundCompletedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.RefundCompleted;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public RefundCompletedHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var refundId   = root.GetProperty("refundId").GetInt32();
        var orderCode  = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() ?? "" : "";
        var customerId = root.GetProperty("customerId").GetInt32();
        var amount     = root.TryGetProperty("amount", out var am) ? am.GetDecimal() : 0;
        var orderId    = root.TryGetProperty("orderId", out var oi) ? oi.GetInt32() : 0;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = customerId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Order,
            TemplateCode       = NotificationTemplates.RefundCompleted,
            Placeholders       = new Dictionary<string, string>
            {
                ["OrderCode"] = orderCode,
                ["Amount"]    = $"{amount:N0}",
            },
            ReferenceId  = $"{refundId}",
            SendBell     = true,
            SendEmail    = true,
            ActionTarget = orderId > 0 ? $"/profile/orders/{orderId}" : "/refunds",
        }, ct);
    }
}
