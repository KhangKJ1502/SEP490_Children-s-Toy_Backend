using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Retries PayOS webhook logs that are in RECEIVED or ERROR state.
/// Runs every 3 minutes.
/// </summary>
public class PayosWebhookRetryJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PayosWebhookRetryJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(3);

    public PayosWebhookRetryJob(IServiceProvider services, ILogger<PayosWebhookRetryJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "PayosWebhookRetryJob error"); }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var webhookService = scope.ServiceProvider.GetRequiredService<IPayOsPayoutWebhookService>();

        var retryable = await db.PayosWebhookLogs
            .Where(l => l.ProcessStatus == "RECEIVED" || l.ProcessStatus == "ERROR")
            .OrderBy(l => l.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        _logger.LogInformation("PayosWebhookRetryJob: found {Count} unprocessed webhooks", retryable.Count);

        foreach (var log in retryable)
        {
            try
            {
                await webhookService.HandleAsync(log.RawPayload, ct);
                _logger.LogInformation("PayosWebhookRetryJob: reprocessed webhook log {Id}", log.WebhookLogId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayosWebhookRetryJob: error reprocessing webhook log {Id}", log.WebhookLogId);
            }
        }
    }
}
