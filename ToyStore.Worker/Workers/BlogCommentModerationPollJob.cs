using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Worker chạy nền định kỳ (mỗi 30 giây) để quét các bình luận/phản hồi Blog ở trạng thái Pending
/// và gửi tới Python AI Sidecar service để thực hiện kiểm duyệt tự động.
/// </summary>
public class BlogCommentModerationPollJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BlogCommentModerationPollJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30); // Chu kỳ quét: 30 giây
    private const int BatchSize = 20; // Số lượng bản ghi kiểm duyệt tối đa trong 1 đợt
    private const int RetryIntervalMinutes = 5; // Khoảng thời gian giãn cách trước khi thử lại bản ghi bị lỗi (5 phút)

    public BlogCommentModerationPollJob(
        IServiceProvider services,
        ILogger<BlogCommentModerationPollJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Vòng lặp liên tục chạy background task đến khi hệ thống phát tín hiệu dừng
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "BlogCommentModerationPollJob error"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }


    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var gateway = scope.ServiceProvider.GetRequiredService<IBlogCommentModerationGateway>();

        bool success = true;
        string? message = null;

        try
        {
            var retryDue = DateTime.UtcNow.AddMinutes(-RetryIntervalMinutes);
            var commentBatchSize = Math.Max(1, BatchSize / 2);
            var replyBatchSize = Math.Max(1, BatchSize - commentBatchSize);

            var commentIds = await db.ReviewBlogs
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                         && x.ModerationStatus == "Pending"
                         && (x.LastRetryAt == null || x.LastRetryAt <= retryDue))
                .OrderBy(x => x.CreatedAt)
                .Select(x => x.ReviewBlogId)
                .Take(commentBatchSize)
                .ToListAsync(ct);

            var replyIds = await db.ReviewBlogReplies
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                         && x.ModerationStatus == "Pending"
                         && (x.LastRetryAt == null || x.LastRetryAt <= retryDue))
                .OrderBy(x => x.CreatedAt)
                .Select(x => x.ReplyBlogId)
                .Take(replyBatchSize)
                .ToListAsync(ct);

            var processed = 0;
            foreach (var commentId in commentIds)
            {
                if (await gateway.ModerateCommentAsync(commentId, ct))
                {
                    processed++;
                }
            }

            foreach (var replyId in replyIds)
            {
                if (await gateway.ModerateReplyAsync(replyId, ct))
                {
                    processed++;
                }
            }

            message = processed > 0
                ? $"Submitted {processed} blog comment moderation item(s)."
                : "No pending blog comment moderation items.";

            if (processed > 0)
            {
                _logger.LogInformation("BlogCommentModerationPollJob: {Message}", message);
            }
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "BlogCommentModerationPollJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(
            scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>(),
            "BlogCommentModerationPollJob", success, message, _logger, ct);
    }
}
