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

        var txnId   = root.TryGetProperty("txnId", out var t) ? t.GetString() ?? "" : "";
        var now     = DateTime.UtcNow;
        var admins  = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 3 }, ct);

        foreach (var admin in admins)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = admin.AccountId,
                RecipientType      = RecipientTypes.Admin,
                NotificationType   = NotificationTypes.System,
                TemplateCode       = NotificationTemplates.AdminPaymentError,
                Placeholders       = new Dictionary<string, string>
                {
                    ["GatewayName"]  = "SePay",
                    ["ErrorMessage"] = $"Txn {txnId} failed",
                },
                ReferenceId  = $"{txnId}:{admin.AccountId}:{now:yyyyMMddHHmm}",
                SendBell     = true,
                SendEmail    = true,
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

        var jobName = root.TryGetProperty("jobName", out var j) ? j.GetString() ?? "Unknown" : "Unknown";
        var now     = DateTime.UtcNow;
        var admins  = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 3 }, ct);

        foreach (var admin in admins)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = admin.AccountId,
                RecipientType      = RecipientTypes.Admin,
                NotificationType   = NotificationTypes.System,
                TemplateCode       = NotificationTemplates.AdminJobFailed,
                Placeholders       = new Dictionary<string, string>
                {
                    ["JobName"] = jobName,
                },
                ReferenceId  = $"{jobName}:{admin.AccountId}:{now:yyyyMMddHHmm}",
                SendBell     = true,
                SendEmail    = true,
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

        var blogId    = root.TryGetProperty("blogId", out var bid) ? bid.GetInt32()
                      : root.TryGetProperty("blogPostId", out var bpid) ? bpid.GetInt32() : 0;
        var blogTitle = root.TryGetProperty("blogTitle", out var bt) ? bt.GetString() ?? $"#{blogId}" : $"#{blogId}";

        var admins = await _unitOfWork.Accounts.GetByRoleIdsAsync(new byte[] { 3 }, ct);

        foreach (var admin in admins)
        {
            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = admin.AccountId,
                RecipientType      = RecipientTypes.Admin,
                NotificationType   = NotificationTypes.Blog,
                TemplateCode       = NotificationTemplates.AdminBlogPending,
                Placeholders       = new Dictionary<string, string>
                {
                    ["BlogTitle"] = blogTitle,
                },
                ReferenceId  = $"{blogId}:{admin.AccountId}",
                SendBell     = true,
                SendEmail    = false,
                ActionTarget = $"/admin/blogs/{blogId}",
            }, ct);
        }
    }
}
