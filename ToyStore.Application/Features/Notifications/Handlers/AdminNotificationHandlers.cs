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
            "Lỗi cổng thanh toán",
            $"Giao dịch {txnId} thất bại tại cổng thanh toán",
            EventType,
            NotificationTemplates.AdminPaymentError,
            ct);
    }

    private async Task NotifyAdminsAsync(string title, string message, string eventKey, string templateCode, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var admins = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 3 }, ct); // Assuming 3 is Admin

        foreach (var admin in admins)
        {
            var adminId = admin.AccountId;
            
            // Simplified throttle check for refactoring
            var recentCount = await _unitOfWork.Deliveries.CountAsync(
                adminId,
                NotificationChannels.Email,
                NotificationStatuses.Unread, ct);

            if (recentCount > 10) continue; // Basic safeguard

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

        var now = DateTime.UtcNow;
        var admins = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 3 }, ct);

        foreach (var admin in admins)
        {
            var adminId = admin.AccountId;
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = adminId,
                RecipientType      = RecipientTypes.Admin,
                NotificationType   = NotificationTypes.System,
                Title              = "Background job lỗi",
                Message            = $"Job '{jobName}' gặp lỗi và cần kiểm tra",
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
        var blogPostId = root.GetProperty("blogPostId").GetInt32();

        var admins = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 3 }, ct);

        foreach (var admin in admins)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = admin.AccountId,
                RecipientType      = RecipientTypes.Admin,
                NotificationType   = NotificationTypes.Blog,
                Title              = "Bài viết chờ duyệt",
                Message            = "Có bài viết mới cần được phê duyệt",
                SendBell           = true,
                SendEmail          = false,
                TemplateCode       = NotificationTemplates.AdminBlogPending,
                ActionTarget       = $"/admin/blogs/{blogPostId}",
                IdempotencyKey     = $"{EventType}:{blogPostId}:{admin.AccountId}",
            }, ct);
        }
    }
}
