using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Worker.Workers;

public class BlogPublishWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlogPublishWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public BlogPublishWorker(IServiceProvider serviceProvider, ILogger<BlogPublishWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
                var updatedCount = await blogRepository.PublishDueScheduledBlogsAsync(DateTime.UtcNow, stoppingToken);

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
