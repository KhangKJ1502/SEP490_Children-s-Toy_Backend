using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;
using ToyStore.Recommendation.Configuration;
using ToyStore.Recommendation.MongoDb;
using ToyStore.Recommendation.MongoDb.Documents;

namespace ToyStore.Recommendation.Jobs;

/// <summary>
/// Background job — mỗi 5 phút đọc các document chưa flush trong MongoDB.session_events,
/// insert sang SQL Server.Interaction.Events, sau đó update flushedAt = now.
/// </summary>
public class FlushEventsJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly RecommendationOptions _options;
    private readonly ILogger<FlushEventsJob> _logger;

    public FlushEventsJob(
        IServiceProvider services,
        IOptions<RecommendationOptions> options,
        ILogger<FlushEventsJob> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "FlushEventsJob starting — interval {Minutes} minutes",
            _options.FlushEventsIntervalMinutes);

        // Chờ 30s khi worker vừa khởi động để app warm-up xong
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FlushEventsJob iteration failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_options.FlushEventsIntervalMinutes), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("FlushEventsJob stopping");
    }

    /// <summary>1 lần chạy — đẩy hết batch chưa flush ra SQL Server.</summary>
    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var mongo = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();

        var batchSize = Math.Max(50, _options.FlushBatchSize);
        var totalFlushed = 0;
        var startUtc = DateTime.UtcNow;

        // Lặp lấy batch cho tới khi không còn doc chưa flush (mỗi batch FlushBatchSize)
        while (!ct.IsCancellationRequested)
        {
            // Filter: flushedAt = null + createdAt nhỏ hơn lúc job start
            // (Tránh đua với insert mới đang chạy song song)
            var filter = Builders<SessionEventDocument>.Filter.And(
                Builders<SessionEventDocument>.Filter.Eq(x => x.FlushedAt, null),
                Builders<SessionEventDocument>.Filter.Lt(x => x.CreatedAt, startUtc));

            var pending = await mongo.SessionEvents
                .Find(filter)
                .Limit(batchSize)
                .ToListAsync(ct);

            if (pending.Count == 0) break;

            // Map MongoDB doc → SQL Server Event entity
            var entities = pending.Select(MapToEntity).ToList();

            // Bulk insert vào SQL Server (1 SaveChanges per batch)
            await db.Events.AddRangeAsync(entities, ct);
            await db.SaveChangesAsync(ct);

            // Update flushedAt = now cho các doc vừa flush
            var ids = pending.Select(p => p.Id).ToList();
            var now = DateTime.UtcNow;
            var update = Builders<SessionEventDocument>.Update.Set(x => x.FlushedAt, now);
            var idFilter = Builders<SessionEventDocument>.Filter.In(x => x.Id, ids);
            await mongo.SessionEvents.UpdateManyAsync(idFilter, update, cancellationToken: ct);

            totalFlushed += pending.Count;

            // Nếu batch không đầy, dừng vòng lặp
            if (pending.Count < batchSize) break;
        }

        if (totalFlushed > 0)
        {
            _logger.LogInformation("FlushEventsJob flushed {Count} events from MongoDB to SQL Server", totalFlushed);
        }
    }

    private static Event MapToEntity(SessionEventDocument doc) => new()
    {
        AccountId = doc.AccountId,
        SessionId = string.IsNullOrWhiteSpace(doc.SessionId) ? "unknown" : doc.SessionId,
        EventType = doc.EventType ?? string.Empty,
        EntityId = string.IsNullOrWhiteSpace(doc.EntityId) ? "0" : doc.EntityId,
        EntityType = doc.EntityType ?? string.Empty,
        Source = doc.Source,
        Referrer = doc.Referrer,
        DeviceType = doc.DeviceType,
        DurationMs = doc.DurationMs,
        ScrollDepth = doc.ScrollDepth,
        ClickPosition = doc.ClickPosition,
        Metadata = doc.Metadata,
        CreatedAt = doc.OccurredAt == default ? doc.CreatedAt : doc.OccurredAt,
    };
}
