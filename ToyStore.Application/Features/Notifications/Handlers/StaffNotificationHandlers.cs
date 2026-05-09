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
        var orderCode    = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() : "";
        var customerName = root.TryGetProperty("customerName", out var cn) ? cn.GetString() : "Customer";

        var staffs = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);

        foreach (var staff in staffs)
        {
            var staffId = staff.AccountId;
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staffId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                Title              = "New refund request",
                Message            = $"{customerName} has requested a refund for order {orderCode}.",
                SendBell           = true,
                SendEmail          = false,
                TemplateCode       = NotificationTemplates.StaffRefundRequest,
                ActionTarget       = $"/admin/refunds/{refundId}",
                IdempotencyKey     = $"{EventType}:{refundId}:{staffId}",
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
        var reviewId = root.GetProperty("reviewId").GetInt32();

        var staffs = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);

        foreach (var staff in staffs)
        {
            var staffId = staff.AccountId;
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staffId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.System,
                Title              = "Review requires manual moderation",
                Message            = "A product review has been flagged for manual moderation.",
                SendBell           = true,
                SendEmail          = false,
                TemplateCode       = NotificationTemplates.StaffReviewModeration,
                ActionTarget       = $"/admin/reviews/{reviewId}",
                IdempotencyKey     = $"{EventType}:{reviewId}:{staffId}",
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

        var staffs = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);

        foreach (var staff in staffs)
        {
            var staffId = staff.AccountId;
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staffId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.System,
                Title              = "Low rating review",
                Message            = $"A customer left a {rating}-star review for product #{productId}. Please check.",
                SendBell           = true,
                SendEmail          = false,
                TemplateCode       = NotificationTemplates.StaffLowRating,
                ActionTarget       = $"/admin/reviews/{reviewId}",
                IdempotencyKey     = $"{EventType}:{reviewId}:{staffId}",
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

        var reviewId  = root.GetProperty("reviewId").GetInt32();
        var accountId = root.GetProperty("accountId").GetInt32();

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Blog,
            Title              = "The store replied to your review",
            Message            = "A staff member has replied to your product review. Check it out!",
            SendBell           = true,
            SendEmail          = false,
            TemplateCode       = NotificationTemplates.ReviewStaffReplied,
            ActionTarget       = $"/products/review/{reviewId}",
            IdempotencyKey     = $"{EventType}:{reviewId}:{accountId}",
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

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Blog,
            Title              = "Someone replied to your comment",
            Message            = "Someone replied to your comment on the blog. Check it out!",
            SendBell           = true,
            SendEmail          = false,
            TemplateCode       = NotificationTemplates.BlogCommentReplied,
            ActionTarget       = $"/blog/{blogPostId}#reply-{replyBlogId}",
            IdempotencyKey     = $"{EventType}:{replyBlogId}:{accountId}",
        }, ct);
    }
}

/// <summary>
/// Notifies staff when a customer requests order cancellation.
/// </summary>
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

        var orderId   = root.GetProperty("orderId").GetInt32();
        var orderCode = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() : $"#{orderId}";
        var reason    = root.TryGetProperty("reason", out var r) ? r.GetString() : "";

        var staffs = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);

        foreach (var staff in staffs)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staff.AccountId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                Title              = "Order cancellation",
                Message            = $"Order {orderCode} has been cancelled. Reason: {reason}",
                SendBell           = true,
                SendEmail          = false,
                ActionTarget       = $"/admin/orders/{orderId}",
                IdempotencyKey     = $"{EventType}:{orderId}:{staff.AccountId}",
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

        var orderId        = root.GetProperty("orderId").GetInt32();
        var orderCode      = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() : $"#{orderId}";
        var targetAccountId = root.GetProperty("targetAccountId").GetInt32();

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = targetAccountId,
            RecipientType      = RecipientTypes.Staff,
            NotificationType   = NotificationTypes.Order,
            Title              = "New order assigned",
            Message            = $"Order {orderCode} has been assigned to you by Admin.",
            SendBell           = true,
            SendEmail          = true,
            TemplateCode       = NotificationTemplates.StaffOrderAssigned,
            ActionTarget       = $"/admin/orders/{orderId}",
            IdempotencyKey     = $"{EventType}:{orderId}:{targetAccountId}",
        }, ct);
    }
}
