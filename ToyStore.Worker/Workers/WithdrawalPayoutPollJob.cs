using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Enums;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Polls PayOS every 2 minutes for any PROCESSING withdrawals that have a PayosPayoutId.
/// On SUCCEEDED → CommitAsync; on FAILED/CANCELLED/REJECTED → RollbackAsync.
/// This is the primary completion mechanism for PayOS Chi hộ, which has no outbound webhook.
/// </summary>
public class WithdrawalPayoutPollJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WithdrawalPayoutPollJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(2);

    public WithdrawalPayoutPollJob(IServiceProvider services, ILogger<WithdrawalPayoutPollJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stagger startup so we don't hammer PayOS immediately on deploy
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "WithdrawalPayoutPollJob error"); }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var syncService = scope.ServiceProvider.GetRequiredService<IWithdrawalPayOsSyncService>();

        var processing = await db.WithdrawalRequests
            .Where(w => w.Status == WithdrawalStatuses.Processing
                        && w.PayosPayoutId != null)
            .Select(w => w.WithdrawalId)
            .ToListAsync(ct);

        if (processing.Count == 0)
        {
            _logger.LogDebug("WithdrawalPayoutPollJob: no PROCESSING withdrawals to poll");
            return;
        }

        _logger.LogInformation("WithdrawalPayoutPollJob: polling {Count} PROCESSING withdrawals", processing.Count);

        foreach (var withdrawalId in processing)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await syncService.SyncAsync(withdrawalId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WithdrawalPayoutPollJob: error syncing withdrawal {Id}", withdrawalId);
            }

            // Brief pause between calls to avoid hammering PayOS API
            await Task.Delay(500, ct);
        }
    }
}
