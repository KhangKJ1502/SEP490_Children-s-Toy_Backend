using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Polls pending product reviews as a safety net and asks the AI sidecar to moderate them.
/// Runs every 60 seconds.
/// </summary>
public class ProductReviewModerationPollJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ProductReviewModerationPollJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);
    private const int BatchSize = 20;

    public ProductReviewModerationPollJob(
        IServiceProvider services,
        ILogger<ProductReviewModerationPollJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "ProductReviewModerationPollJob error"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var gateway = scope.ServiceProvider.GetRequiredService<IProductReviewModerationGateway>();

        bool success = true;
        string? message = null;

        try
        {
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

        await BackgroundJobTelemetry.RecordAsync(
            scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>(),
            "ProductReviewModerationPollJob", success, message, _logger, ct);
    }
}
