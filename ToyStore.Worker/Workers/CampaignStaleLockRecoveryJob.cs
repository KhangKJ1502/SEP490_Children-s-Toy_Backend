using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Resets <c>CampaignSchedules</c> stuck in <c>Dispatched</c> with an old <c>LockedAt</c> (worker crash).
/// Runs every 5 minutes.
/// </summary>
public sealed class CampaignStaleLockRecoveryJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CampaignStaleLockRecoveryJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    /// <summary>Locks older than this are treated as stale.</summary>
    private static readonly TimeSpan StaleLockAge = TimeSpan.FromMinutes(30);

    public CampaignStaleLockRecoveryJob(
        IServiceProvider services,
        ILogger<CampaignStaleLockRecoveryJob> logger,
        ITimeProvider timeProvider)
    {
        _services     = services;
        _logger       = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "CampaignStaleLockRecoveryJob error"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db  = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var success = true;
        string? message = null;

        try
        {
            var now = _timeProvider.UtcNow;
            var n   = await uow.Campaigns.RecoverStaleDispatchLocksAsync(StaleLockAge, now, ct);
            if (n > 0)
                _logger.LogWarning("Recovered {Count} stale campaign dispatch lock(s)", n);

            message = $"Recovered {n} stale dispatch lock(s)";
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "CampaignStaleLockRecoveryJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(
            db,
            nameof(CampaignStaleLockRecoveryJob),
            success,
            message,
            _logger,
            ct);
    }
}
