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

        var refundId   = root.GetProperty("refundId").GetInt32();
        var orderCode  = root.TryGetProperty("orderCode", out var oc) ? oc.GetString() : "";
        var customerName = root.TryGetProperty("customerName", out var cn) ? cn.GetString() : "";

        var staffs = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);

        foreach (var staff in staffs)
        {
            var staffId = staff.AccountId;
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staffId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.Order,
                Title              = "Yêu cầu hoàn tiền mới",
                Message            = $"{customerName} yêu cầu hoàn tiền cho đơn {orderCode}",
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
                Title              = "Review cần kiểm duyệt",
                Message            = "Có đánh giá sản phẩm cần được kiểm duyệt thủ công",
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
        var reviewId = root.GetProperty("reviewId").GetInt32();
        var rating   = root.TryGetProperty("rating", out var r) ? r.GetInt32() : 0;

        var staffs = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 2 }, ct);

        foreach (var staff in staffs)
        {
            var staffId = staff.AccountId;
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = staffId,
                RecipientType      = RecipientTypes.Staff,
                NotificationType   = NotificationTypes.System,
                Title              = "Đánh giá thấp",
                Message            = $"Khách hàng vừa để lại đánh giá {rating} sao",
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
            Title              = "Shop đã phản hồi đánh giá của bạn",
            Message            = "Shop vừa trả lời đánh giá của bạn. Xem ngay!",
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

        var accountId  = root.GetProperty("accountId").GetInt32();
        var replyBlogId = root.GetProperty("replyBlogId").GetInt32();
        var blogPostId  = root.TryGetProperty("blogPostId", out var b) ? b.GetInt32() : 0;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Blog,
            Title              = "Có người phản hồi bình luận của bạn",
            Message            = "Ai đó vừa trả lời bình luận của bạn trên blog",
            SendBell           = true,
            SendEmail          = false,
            TemplateCode       = NotificationTemplates.BlogCommentReplied,
            ActionTarget       = $"/blog/{blogPostId}#reply-{replyBlogId}",
            IdempotencyKey     = $"{EventType}:{replyBlogId}:{accountId}",
        }, ct);
    }
}
