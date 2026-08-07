using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Worker chạy nền định kỳ (mỗi 1 giờ) để tự động từ chối (Auto Reject) các bình luận / phản hồi Blog
/// bị tồn đọng ở trạng thái Chờ duyệt tay (ManualReview) quá 24 giờ mà Admin chưa kịp xử lý.
/// </summary>
public class BlogCommentManualReviewTimeoutJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BlogCommentManualReviewTimeoutJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Chu kỳ chạy: 1 giờ
    private const string TimeoutBanReasonContent =
        "Manual review was not completed within 24 hours, so the comment was automatically rejected";
    private const string TimeoutBanReasonPrefix = "Manual review was not completed within 24 hours";
    private const int ViolationThreshold = 20; // Ngưỡng vi phạm quá hạn để tính khóa tài khoản
    private const int CommentBanDurationDays = 7; // Thời hạn khóa phạt 7 ngày

    public BlogCommentManualReviewTimeoutJob(
        IServiceProvider services,
        ILogger<BlogCommentManualReviewTimeoutJob> logger,
        ITimeProvider timeProvider)
    {
        _services = services;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "BlogCommentManualReviewTimeoutJob error"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }


    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

        bool success = true;
        string? message = null;

        try
        {
            var nowUtc = _timeProvider.UtcNow;
            var comments = await db.ReviewBlogs
                .Where(x => !x.IsDeleted
                         && x.ModerationStatus == "ManualReview"
                         && x.ManualReviewDeadline != null
                         && x.ManualReviewDeadline <= nowUtc)
                .ToListAsync(ct);

            var replies = await db.ReviewBlogReplies
                .Where(x => !x.IsDeleted
                         && x.ModerationStatus == "ManualReview"
                         && x.ManualReviewDeadline != null
                         && x.ManualReviewDeadline <= nowUtc)
                .ToListAsync(ct);

            var timeoutBanReasonId = comments.Count + replies.Count > 0
                ? await GetOrCreateTimeoutBanReasonIdAsync(db, nowUtc, ct)
                : (byte?)null;

            foreach (var comment in comments)
            {
                comment.ModerationStatus = "Rejected";
                comment.ManualReviewDeadline = null;
                comment.UpdatedAt = nowUtc;

                db.BlogCommentModerationLogs.Add(CreateTimeoutLog(
                    "Comment", comment.ReviewBlogId, null, timeoutBanReasonId!.Value, nowUtc));
                await db.SaveChangesAsync(ct);
                await NotifyAutoRejectedAsync(dispatcher, comment.AccountId, "Comment", comment.ReviewBlogId, ct);
                await RegisterViolationAndLockIfNeededAsync(db, dispatcher, comment.AccountId, nowUtc, ct);
            }

            foreach (var reply in replies)
            {
                reply.ModerationStatus = "Rejected";
                reply.ManualReviewDeadline = null;
                reply.UpdatedAt = nowUtc;

                db.BlogCommentModerationLogs.Add(CreateTimeoutLog(
                    "Reply", null, reply.ReplyBlogId, timeoutBanReasonId!.Value, nowUtc));
                await db.SaveChangesAsync(ct);
                await NotifyAutoRejectedAsync(dispatcher, reply.AccountId, "Reply", reply.ReplyBlogId, ct);
                await RegisterViolationAndLockIfNeededAsync(db, dispatcher, reply.AccountId, nowUtc, ct);
            }

            var processed = comments.Count + replies.Count;
            if (processed > 0)
            {
                await db.SaveChangesAsync(ct);
            }

            message = processed > 0
                ? $"Auto-rejected {processed} expired manual review blog comment item(s)."
                : "No expired manual review blog comment items.";

            if (processed > 0)
            {
                _logger.LogInformation("BlogCommentManualReviewTimeoutJob: {Message}", message);
            }
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "BlogCommentManualReviewTimeoutJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(
            scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>(),
            "BlogCommentManualReviewTimeoutJob", success, message, _logger, ct);
    }

    private static async Task<byte> GetOrCreateTimeoutBanReasonIdAsync(
        SEP490ToyStoreContext db,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var reason = await db.BlogCommentBanReasons
            .Where(x => x.Content.Contains(TimeoutBanReasonPrefix))
            .OrderBy(x => x.BanReasonId)
            .FirstOrDefaultAsync(ct);

        if (reason is not null)
        {
            return reason.BanReasonId;
        }

        reason = new BlogCommentBanReason
        {
            Content = TimeoutBanReasonContent,
            CreatedAt = nowUtc
        };

        db.BlogCommentBanReasons.Add(reason);
        await db.SaveChangesAsync(ct);
        return reason.BanReasonId;
    }

    private static BlogCommentModerationLog CreateTimeoutLog(
        string targetType,
        int? commentId,
        int? replyId,
        byte banReasonId,
        DateTime nowUtc)
    {
        return new BlogCommentModerationLog
        {
            TargetType = targetType,
            CommentId = commentId,
            ReplyId = replyId,
            ModeratorType = "System",
            ModeratedBy = null,
            Action = "Rejected",
            BanReasonId = banReasonId,
            ModerationResult = JsonSerializer.Serialize(new { reason = "manual_review_timeout_24h" }),
            CreatedAt = nowUtc
        };
    }

    private static Task NotifyAutoRejectedAsync(
        INotificationDispatcher dispatcher,
        int accountId,
        string targetType,
        int targetId,
        CancellationToken ct)
    {
        return dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType = RecipientTypes.Customer,
            NotificationType = NotificationTypes.System,
            Title = "Comment rejected",
            Message = "Your comment was rejected because it was not reviewed within 24 hours.",
            SendBell = true,
            SendEmail = false,
            ActionTarget = "/blog",
            IdempotencyKey = $"blog-comment:auto-timeout:{targetType}:{targetId}"
        }, ct);
    }

    private async Task RegisterViolationAndLockIfNeededAsync(
        SEP490ToyStoreContext db,
        INotificationDispatcher dispatcher,
        int accountId,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var state = await db.BlogCommentViolationCounts
            .FirstOrDefaultAsync(x => x.AccountId == accountId, ct);

        if (state == null)
        {
            state = new BlogCommentViolationCount
            {
                AccountId = accountId,
                ViolationCount = 0,
                UpdatedAt = nowUtc,
                IsCommentBanned = false,
                RateCount = 0
            };
            await db.BlogCommentViolationCounts.AddAsync(state, ct);
        }

        if (state.ViolationCount < byte.MaxValue)
        {
            state.ViolationCount++;
        }

        state.LastViolatedAt = nowUtc;
        state.UpdatedAt = nowUtc;

        if (state.ViolationCount < ViolationThreshold)
        {
            await db.SaveChangesAsync(ct);
            return;
        }

        if (!state.IsCommentBanned)
        {
            state.IsCommentBanned = true;
            state.BannedAt = nowUtc;
        }

        state.BanExpiresAt = nowUtc.AddDays(CommentBanDurationDays);
        state.UpdatedAt = nowUtc;

        await db.SaveChangesAsync(ct);

        await dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType = RecipientTypes.Customer,
            NotificationType = NotificationTypes.System,
            Title = "Commenting locked",
            Message = "Your commenting access has been locked due to repeated violations.",
            SendBell = true,
            SendEmail = false,
            ActionTarget = "/blog",
            IdempotencyKey = $"blog-comment:lock:{accountId}"
        }, ct);
    }
}
