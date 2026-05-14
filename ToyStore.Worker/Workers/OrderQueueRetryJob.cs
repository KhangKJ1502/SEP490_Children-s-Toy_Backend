using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Worker.Workers;

public class OrderQueueRetryJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OrderQueueRetryJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(2);

    public OrderQueueRetryJob(IServiceProvider services, ILogger<OrderQueueRetryJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderQueueRetryJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrderQueueRetryJob error");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var assignmentService = scope.ServiceProvider.GetRequiredService<IShiftAssignmentService>();
        await assignmentService.TryAssignOldestQueueAsync(ct);
    }
}
