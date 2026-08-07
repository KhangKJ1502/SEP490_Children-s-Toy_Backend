using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Worker chạy nền định kỳ (mỗi 1 phút) để xuất bản tự động các bài viết Blog đã lên lịch công bố (Scheduled Publish Time).
/// </summary>
public class BlogPublishWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlogPublishWorker> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // Chu kỳ chạy: 1 phút

    public BlogPublishWorker(
        IServiceProvider serviceProvider, 
        ILogger<BlogPublishWorker> logger,
        ITimeProvider timeProvider)
    {
        _serviceProvider = serviceProvider;
        _logger          = logger;
        _timeProvider    = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Blog Publish Worker starting");


        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var blogRepository = scope.ServiceProvider.GetRequiredService<IBlogRepository>();
                var updatedCount = await blogRepository.PublishDueScheduledBlogsAsync(_timeProvider.UtcNow, stoppingToken);

                if (updatedCount > 0)
                {
                    _logger.LogInformation("Auto-published {Count} scheduled blog(s).", updatedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when auto-publishing scheduled blogs.");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Blog Publish Worker stopping");
    }
}
