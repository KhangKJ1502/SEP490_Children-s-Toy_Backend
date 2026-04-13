using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Background worker that periodically recalculates recommendation scores.
/// </summary>
public class RecommendationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecommendationWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);
    
    public RecommendationWorker(IServiceProvider serviceProvider, ILogger<RecommendationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recommendation Worker starting");
        
        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RecalculateRecommendationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating recommendations");
            }
            
            await Task.Delay(_interval, stoppingToken);
        }
        
        _logger.LogInformation("Recommendation Worker stopping");
    }
    
    private async Task RecalculateRecommendationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var recommendationService = scope.ServiceProvider.GetRequiredService<IRecommendationService>();
        
        _logger.LogInformation("Starting recommendation score recalculation");
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        await recommendationService.RecalculateRecommendationScoresAsync(cancellationToken);
        
        stopwatch.Stop();
        
        _logger.LogInformation(
            "Recommendation score recalculation completed in {ElapsedMs}ms",
            stopwatch.ElapsedMilliseconds);
    }
}
