using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Polls pending blog comments/replies and asks the AI sidecar to moderate them.
/// Runs every 30 seconds.
/// </summary>
public class BlogCommentModerationPollJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BlogCommentModerationPollJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 20;
    private const int RetryIntervalMinutes = 5;

    public BlogCommentModerationPollJob(
        IServiceProvider services,
        ILogger<BlogCommentModerationPollJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
