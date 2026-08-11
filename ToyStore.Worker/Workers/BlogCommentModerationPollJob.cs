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


    /// <summary>
    /// Thực hiện quét định kỳ các bình luận/phản hồi Blog có trạng thái Pending để đẩy qua cổng kiểm duyệt AI.
    /// </summary>
    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var gateway = scope.ServiceProvider.GetRequiredService<IBlogCommentModerationGateway>();

        bool success = true;
        string? message = null;

        try
        {
            // Xác định thời điểm tối đa được phép thử lại (tránh spam API liên tục khi bị lỗi)
            var retryDue = DateTime.UtcNow.AddMinutes(-RetryIntervalMinutes);
            
            // Phân bổ kích thước lô (Batch Size = 20) chia đều cho bình luận (10) và phản hồi (10)
            var commentBatchSize = Math.Max(1, BatchSize / 2);
            var replyBatchSize = Math.Max(1, BatchSize - commentBatchSize);

            // 1. Quét danh sách ID bình luận chưa xóa ở trạng thái Chờ duyệt (Pending) và thỏa mãn khoảng thời gian thử lại
            var commentIds = await db.ReviewBlogs
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                         && x.ModerationStatus == "Pending"
                         && (x.LastRetryAt == null || x.LastRetryAt <= retryDue))
                .OrderBy(x => x.CreatedAt)
                .Select(x => x.ReviewBlogId)
                .Take(commentBatchSize)
                .ToListAsync(ct);

            // 2. Quét danh sách ID phản hồi bình luận chưa xóa ở trạng thái Chờ duyệt (Pending) và thỏa mãn thời gian thử lại
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
            
            // 3. Gọi AI Gateway xử lý duyệt tự động cho từng bình luận tìm thấy
            foreach (var commentId in commentIds)
            {
                if (await gateway.ModerateCommentAsync(commentId, ct))
                {
                    processed++;
                }
            }

            // 4. Gọi AI Gateway xử lý duyệt tự động cho từng phản hồi bình luận tìm thấy
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

        // 5. Ghi nhận Telemetry đo lường hoạt động của background job
        await BackgroundJobTelemetry.RecordAsync(
            scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>(),
            "BlogCommentModerationPollJob", success, message, _logger, ct);
    }
}
