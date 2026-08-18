using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Background Worker quét định kỳ (mỗi 60 giây) các đánh giá sản phẩm đang ở trạng thái chờ duyệt ("Pending")
/// đóng vai trò là chốt an toàn (Safety Net) gửi yêu cầu sang AI Moderation Sidecar để kiểm duyệt nội dung tự động nếu sự kiện tức thời bị bỏ lỡ.
/// </summary>
public class ProductReviewModerationPollJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ProductReviewModerationPollJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);
    private const int BatchSize = 20;

    /// <summary>
    /// Khởi tạo ProductReviewModerationPollJob.
    /// </summary>
    public ProductReviewModerationPollJob(
        IServiceProvider services,
        ILogger<ProductReviewModerationPollJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Vòng lặp thực thi ngầm định kỳ của BackgroundService.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "ProductReviewModerationPollJob error"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    /// <summary>
    /// Logic quét và gửi từng batch đánh giá ở trạng thái "Pending" sang AI Gateway để kiểm duyệt.
    /// </summary>
    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var gateway = scope.ServiceProvider.GetRequiredService<IProductReviewModerationGateway>();

        bool success = true;
        string? message = null;

        try
        {
            // Lấy tối đa BatchSize (20) đánh giá đang ở trạng thái Pending cũ nhất
            var reviewIds = await db.ReviewProducts
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                         && x.ModerationStatus == "Pending")
                .OrderBy(x => x.CreatedAt)
                .Select(x => x.ReviewId)
                .Take(BatchSize)
                .ToListAsync(ct);

            var processed = 0;
            foreach (var reviewId in reviewIds)
            {
                if (await gateway.ModerateReviewAsync(reviewId, ct))
                {
                    processed++;
                }
            }

            message = processed > 0
                ? $"Submitted {processed} product review moderation item(s)."
                : "No pending product review moderation items.";

            if (processed > 0)
            {
                _logger.LogInformation("ProductReviewModerationPollJob: {Message}", message);
            }
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "ProductReviewModerationPollJob failed");
        }

        // Ghi nhận telemetry nhật ký chạy của Background Job
        await BackgroundJobTelemetry.RecordAsync(
            scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>(),
            "ProductReviewModerationPollJob", success, message, _logger, ct);
    }
}
