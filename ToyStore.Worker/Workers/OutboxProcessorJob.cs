using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Worker.Workers;

public class OutboxProcessorJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OutboxProcessorJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private const int MaxAttempts = 5;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    public OutboxProcessorJob(
        IServiceProvider services, 
        ILogger<OutboxProcessorJob> logger,
        ITimeProvider timeProvider)
    {
        _services     = services;
        _logger       = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in OutboxProcessorJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope   = _services.CreateScope();
        var db            = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var handlers      = scope.ServiceProvider.GetServices<IOutboxEventHandler>()
                              .ToLookup(h => h.EventType, StringComparer.OrdinalIgnoreCase);

        var lockId = Guid.NewGuid();
        var now    = _timeProvider.UtcNow;

        // Claim a batch of unprocessed events with optimistic locking
        var batch = await db.DomainEventOutboxes
            .Where(e => e.ProcessedOn == null
                     && e.Attempts    < MaxAttempts
                     && (e.ProcessingAt == null || e.ProcessingAt < now.AddMinutes(-5)))
            .OrderBy(e => e.OccurredOn)
            .Take(20)
            .ToListAsync(ct);

        if (batch.Count == 0) return;

        foreach (var ev in batch)
        {
            ev.ProcessingLockId = lockId;
            ev.ProcessingAt     = now;
            ev.Attempts++;
        }
        await db.SaveChangesAsync(ct);

        foreach (var ev in batch)
        {
            var matchingHandlers = handlers[ev.EventType].ToList();
            if (matchingHandlers.Count == 0)
            {
                _logger.LogDebug("No handler for OutboxEvent EventType={EventType}", ev.EventType);
                ev.ProcessedOn = _timeProvider.UtcNow;
                continue;
            }

            try
            {
                var data = new OutboxEventData(
                    ev.EventId,
                    ev.AggregateType,
                    ev.AggregateId,
                    ev.EventType,
                    ev.Payload,
                    ev.OccurredOn);

                foreach (var handler in matchingHandlers)
                {
                    await handler.HandleAsync(data, ct);
                }

                ev.ProcessedOn = _timeProvider.UtcNow;
                ev.LastError   = null;
                _logger.LogInformation(
                    "Outbox event processed. EventType={EventType} EventId={EventId} Handlers={Count}",
                    ev.EventType, ev.EventId, matchingHandlers.Count);
            }
            catch (Exception ex)
            {
                ev.LastError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                _logger.LogError(ex,
                    "Failed to process OutboxEvent EventType={EventType} EventId={EventId} Attempt={Attempt}",
                    ev.EventType, ev.EventId, ev.Attempts);

                if (ev.Attempts >= MaxAttempts)
                {
                    // Notify admin — fire admin alert via a separate scope to avoid db state issues
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var alertScope = _services.CreateScope();
                            var dispatcher = alertScope.ServiceProvider
                                .GetRequiredService<INotificationDispatcher>();
                            await NotifyAdminOutboxStuckAsync(dispatcher, ev.EventType, ev.EventId, ct);
                        }
                        catch { /* best effort */ }
                    }, CancellationToken.None);
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task NotifyAdminOutboxStuckAsync(
        INotificationDispatcher dispatcher,
        string eventType,
        Guid eventId,
        CancellationToken ct)
    {
        // Fetch admin account IDs elsewhere; for simplicity use a broadcast approach
        // The real implementation resolves admin accounts from DB
        await dispatcher.DispatchAsync(new Application.DTOs.Notifications.NotificationContext
        {
            RecipientAccountId = 1, // placeholder — replaced by AdminNotificationService
            RecipientType      = Application.Constants.RecipientTypes.Admin,
            NotificationType   = Application.Constants.NotificationTypes.System,
            Title              = "Outbox event stuck",
            Message            = $"Event {eventType} ({eventId}) reached max retry attempts",
            SendBell           = true,
            SendEmail          = true,
            IdempotencyKey     = $"system.outbox_max_attempts:{eventId}",
        }, ct);
    }
}
