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


    /// <summary>
    /// Thực hiện quét và tự động xử lý từ chối các bình luận/phản hồi chờ duyệt tay quá 24h.
    /// </summary>
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
            
            // 1. Tìm các bình luận (Comments) chưa xóa, ở trạng thái Chờ duyệt tay (ManualReview) và đã quá hạn duyệt deadline
            var comments = await db.ReviewBlogs
                .Where(x => !x.IsDeleted
                         && x.ModerationStatus == "ManualReview"
                         && x.ManualReviewDeadline != null
                         && x.ManualReviewDeadline <= nowUtc)
                .ToListAsync(ct);

            // 2. Tìm các phản hồi bình luận (Replies) chưa xóa, ở trạng thái Chờ duyệt tay (ManualReview) và đã quá hạn duyệt
            var replies = await db.ReviewBlogReplies
                .Where(x => !x.IsDeleted
                         && x.ModerationStatus == "ManualReview"
                         && x.ManualReviewDeadline != null
                         && x.ManualReviewDeadline <= nowUtc)
                .ToListAsync(ct);

            // 3. Lấy hoặc khởi tạo ID lý do cấm tự động do hệ thống hết hạn duyệt 24 giờ
            var timeoutBanReasonId = comments.Count + replies.Count > 0
                ? await GetOrCreateTimeoutBanReasonIdAsync(db, nowUtc, ct)
                : (byte?)null;

            // 4. Xử lý từ chối tự động cho các bình luận quá hạn
            foreach (var comment in comments)
            {
                comment.ModerationStatus = "Rejected"; // Đổi trạng thái sang Rejected
                comment.ManualReviewDeadline = null;   // Xóa deadline chờ duyệt tay
                comment.UpdatedAt = nowUtc;

                // Thêm bản ghi nhật ký kiểm duyệt tự động bởi hệ thống
                db.BlogCommentModerationLogs.Add(CreateTimeoutLog(
                    "Comment", comment.ReviewBlogId, null, timeoutBanReasonId!.Value, nowUtc));
                await db.SaveChangesAsync(ct);
                
                // Gửi thông báo đến tài khoản người dùng báo tin nhắn bị từ chối do quá hạn duyệt
                await NotifyAutoRejectedAsync(dispatcher, comment.AccountId, "Comment", comment.ReviewBlogId, ct);
                
                // Tính điểm phạt vi phạm và tiến hành khóa tạm thời nếu đạt ngưỡng
                await RegisterViolationAndLockIfNeededAsync(db, dispatcher, comment.AccountId, nowUtc, ct);
            }

            // 5. Xử lý từ chối tự động cho các phản hồi bình luận quá hạn
            foreach (var reply in replies)
            {
                reply.ModerationStatus = "Rejected"; // Đổi trạng thái sang Rejected
                reply.ManualReviewDeadline = null;   // Xóa deadline chờ duyệt
                reply.UpdatedAt = nowUtc;

                // Thêm bản ghi nhật ký kiểm duyệt tự động bởi hệ thống
                db.BlogCommentModerationLogs.Add(CreateTimeoutLog(
                    "Reply", null, reply.ReplyBlogId, timeoutBanReasonId!.Value, nowUtc));
                await db.SaveChangesAsync(ct);
                
                // Gửi thông báo đến tài khoản người dùng
                await NotifyAutoRejectedAsync(dispatcher, reply.AccountId, "Reply", reply.ReplyBlogId, ct);
                
                // Tính điểm phạt vi phạm
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

        // 6. Ghi nhận Telemetry đo lường hoạt động của background job
        await BackgroundJobTelemetry.RecordAsync(
            scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>(),
            "BlogCommentManualReviewTimeoutJob", success, message, _logger, ct);
    }

    /// <summary>
    /// Lấy hoặc tạo mới lý do cấm tự động liên quan đến lỗi quá hạn duyệt 24 giờ của Admin.
    /// </summary>
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

    /// <summary>
    /// Tạo đối tượng nhật ký kiểm duyệt tự động do quá hạn 24 giờ.
    /// </summary>
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
            ModeratorType = "System", // Thực hiện tự động bởi hệ thống
            ModeratedBy = null,
            Action = "Rejected",
            BanReasonId = banReasonId,
            ModerationResult = JsonSerializer.Serialize(new { reason = "manual_review_timeout_24h" }),
            CreatedAt = nowUtc
        };
    }

    /// <summary>
    /// Gửi thông báo hệ thống tự động từ chối bình luận do hết hạn duyệt tay.
    /// </summary>
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

    /// <summary>
    /// Ghi nhận điểm vi phạm vào tài khoản người dùng và tự động khóa tính năng comment nếu đạt ngưỡng quy định.
    /// </summary>
    private async Task RegisterViolationAndLockIfNeededAsync(
        SEP490ToyStoreContext db,
        INotificationDispatcher dispatcher,
        int accountId,
        DateTime nowUtc,
        CancellationToken ct)
    {
        // Lấy thông tin vi phạm của người dùng từ Database
        var state = await db.BlogCommentViolationCounts
            .FirstOrDefaultAsync(x => x.AccountId == accountId, ct);

        // Khởi tạo mới nếu người dùng chưa có dòng vi phạm nào
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

        // Tăng số lần vi phạm lên 1 (tối đa là byte.MaxValue)
        if (state.ViolationCount < byte.MaxValue)
        {
            state.ViolationCount++;
        }

        state.LastViolatedAt = nowUtc;
        state.UpdatedAt = nowUtc;

        // Nếu số lần vi phạm chưa vượt quá ngưỡng khóa phạt (threshold = 20), lưu và thoát
        if (state.ViolationCount < ViolationThreshold)
        {
            await db.SaveChangesAsync(ct);
            return;
        }

        // Nếu đạt ngưỡng, tiến hành khóa quyền bình luận
        if (!state.IsCommentBanned)
        {
            state.IsCommentBanned = true;
            state.BannedAt = nowUtc;
        }

        // Đặt hạn cấm bình luận trong 7 ngày
        state.BanExpiresAt = nowUtc.AddDays(CommentBanDurationDays);
        state.UpdatedAt = nowUtc;

        await db.SaveChangesAsync(ct);

        // Gửi thông báo đến tài khoản người dùng báo tin đã bị khóa quyền bình luận
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
