using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Features.Notifications.Handlers;

public class PaymentGatewayErrorHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.SystemPaymentGatewayError;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public PaymentGatewayErrorHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;
        var txnId = root.TryGetProperty("txnId", out var t) ? t.GetString() : "";

        await NotifyAdminsAsync(
            "Payment gateway error",
            $"Transaction {txnId} failed at the payment gateway. Please investigate.",
            EventType,
            NotificationTemplates.AdminPaymentError,
            ct);
    }

    private async Task NotifyAdminsAsync(string title, string message, string eventKey, string templateCode, CancellationToken ct)
    {
        var now    = DateTime.UtcNow;
        var admins = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 3 }, ct);

        foreach (var admin in admins)
        {
            var adminId = admin.AccountId;
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = adminId,
                RecipientType      = RecipientTypes.Admin,
                NotificationType   = NotificationTypes.System,
                Title              = title,
                Message            = message,
                SendBell           = true,
                SendEmail          = true,
                TemplateCode       = templateCode,
                IdempotencyKey     = $"{eventKey}:{adminId}:{now:yyyyMMddHHmm}",
            }, ct);
        }
    }
}

public class BackgroundJobFailedHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.SystemBackgroundJobFailed;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public BackgroundJobFailedHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;
        var jobName = root.TryGetProperty("jobName", out var j) ? j.GetString() : "Unknown";

        var now    = DateTime.UtcNow;
        var admins = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 3 }, ct);

        foreach (var admin in admins)
        {
            var adminId = admin.AccountId;
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = adminId,
                RecipientType      = RecipientTypes.Admin,
                NotificationType   = NotificationTypes.System,
                Title              = "Background job failed",
                Message            = $"Job '{jobName}' encountered an error and requires attention.",
                SendBell           = true,
                SendEmail          = true,
                TemplateCode       = NotificationTemplates.AdminJobFailed,
                IdempotencyKey     = $"{EventType}:{jobName}:{adminId}:{now:yyyyMMddHHmm}",
            }, ct);
        }
    }
}

public class BlogPendingApprovalHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.ContentBlogPendingApproval;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;

    public BlogPendingApprovalHandler(IUnitOfWork unitOfWork, INotificationDispatcher dispatcher)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var blogId   = root.TryGetProperty("blogId", out var bid) ? bid.GetInt32()
                     : root.TryGetProperty("blogPostId", out var bpid) ? bpid.GetInt32() : 0;
        var authorId = root.TryGetProperty("authorId", out var aid) ? aid.GetInt32() : 0;

        var admins = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 3 }, ct);

        foreach (var admin in admins)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = admin.AccountId,
                RecipientType      = RecipientTypes.Admin,
                NotificationType   = NotificationTypes.Blog,
                Title              = "Blog post pending approval",
                Message            = "A new blog post is waiting for your approval.",
                SendBell           = true,
                SendEmail          = false,
                TemplateCode       = NotificationTemplates.AdminBlogPending,
                ActionTarget       = $"/admin/blogs/{blogId}",
                IdempotencyKey     = $"{EventType}:{blogId}:{admin.AccountId}",
            }, ct);
        }
    }
}
