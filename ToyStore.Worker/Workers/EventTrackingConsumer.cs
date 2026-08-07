using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;
using ToyStore.Recommendation.RabbitMq;

namespace ToyStore.Worker.Workers;

public class EventTrackingConsumer : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EventTrackingConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    
    private const string QueueName = "tracking_events";
    private readonly List<BasicGetResult> _batch = new();
    private readonly int _batchSize = 500;
    private readonly TimeSpan _batchTimeout = TimeSpan.FromSeconds(5);

    public EventTrackingConsumer(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<EventTrackingConsumer> logger)
    {
        _services = services;
        _configuration = configuration;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        InitRabbitMq();
        return base.StartAsync(cancellationToken);
    }

    private void InitRabbitMq()
    {
        CleanupRabbitMq();

        var host = _configuration["RabbitMq:Host"] ?? "localhost";
        var port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672");
        var username = _configuration["RabbitMq:UserName"] ?? "guest";
        var password = _configuration["RabbitMq:Password"] ?? "guest";

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = username,
            Password = password,
            DispatchConsumersAsync = true
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            // QoS: Lấy tối đa BatchSize tin nhắn cùng lúc (chưa ACK)
            _channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)_batchSize, global: false);
            _logger.LogInformation("Successfully initialized RabbitMQ connection in EventTrackingConsumer.");
        }
        catch (Exception ex)
        {
            CleanupRabbitMq();
            _logger.LogWarning(ex, "Failed to initialize RabbitMQ connection in Consumer. Will retry later.");
        }
    }

    private void CleanupRabbitMq()
    {
        try { _channel?.Dispose(); } catch { }
        try { _connection?.Dispose(); } catch { }
        _channel = null;
        _connection = null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_channel == null || !_channel.IsOpen)
            {
                InitRabbitMq();
                if (_channel == null || !_channel.IsOpen)
                {
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }
            }

            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event batch in EventTrackingConsumer. Requeuing messages.");
                if (_batch.Count > 0 && _channel != null && _channel.IsOpen)
                {
                    foreach (var msg in _batch)
                    {
                        try
                        {
                            _channel.BasicNack(msg.DeliveryTag, false, requeue: true);
                        }
                        catch { }
                    }
                }
                _batch.Clear();
                CleanupRabbitMq();
                await Task.Delay(3000, stoppingToken);
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        _batch.Clear();

        // Thu thập batch (dựa trên size hoặc time)
        while (_batch.Count < _batchSize && (DateTime.UtcNow - startTime) < _batchTimeout)
        {
            if (ct.IsCancellationRequested) break;

            var result = _channel!.BasicGet(QueueName, autoAck: false);
            if (result != null)
            {
                _batch.Add(result);
            }
            else
            {
                // Hết message trong queue, thoát vòng lặp nhỏ để chờ
                await Task.Delay(500, ct); 
            }
        }

        if (_batch.Count == 0) return;

        // Xử lý batch
        var messagesToSave = new List<TrackEventMessage>();
        foreach (var msg in _batch)
        {
            try
            {
                var body = Encoding.UTF8.GetString(msg.Body.ToArray());
                var eventMsg = JsonSerializer.Deserialize<TrackEventMessage>(body);
                if (eventMsg != null) messagesToSave.Add(eventMsg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize event message");
                _channel!.BasicNack(msg.DeliveryTag, false, requeue: false); // Invalid message, discard
            }
        }

        if (messagesToSave.Count > 0)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();

            var uids = messagesToSave.Select(x => x.IdempotencyKey).ToList();
            var existingUids = await db.Events
                .Where(e => uids.Contains(e.IdempotencyKey))
                .Select(e => e.IdempotencyKey)
                .ToListAsync(ct);

            var entities = messagesToSave
                .Where(m => !existingUids.Contains(m.IdempotencyKey))
                .Select(MapToEntity)
                .ToList();

            if (entities.Count > 0)
            {
                try
                {
                    await db.Events.AddRangeAsync(entities, ct);
                    await db.SaveChangesAsync(ct);
                    _logger.LogInformation("EventTrackingConsumer: Saved {Count} new events to SQL Server.", entities.Count);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogWarning(ex, "Batch save failed for events. Retrying entities individually...");
                    db.ChangeTracker.Clear();

                    int savedCount = 0;
                    foreach (var entity in entities)
                    {
                        try
                        {
                            await db.Events.AddAsync(entity, ct);
                            await db.SaveChangesAsync(ct);
                            savedCount++;
                        }
                        catch (Exception itemEx)
                        {
                            _logger.LogError(itemEx, "Failed to save individual event with IdempotencyKey={Key}, EntityId={EntityId}", entity.IdempotencyKey, entity.EntityId);
                            db.ChangeTracker.Clear();
                        }
                    }
                    _logger.LogInformation("EventTrackingConsumer: Saved {SavedCount}/{TotalCount} events after individual retries.", savedCount, entities.Count);
                }
            }
        }

        // ACK toàn bộ batch
        foreach (var msg in _batch)
        {
            _channel!.BasicAck(msg.DeliveryTag, multiple: false);
        }
        _batch.Clear();
    }

    private static Event MapToEntity(TrackEventMessage doc) => new()
    {
        IdempotencyKey = doc.IdempotencyKey,
        AccountId = doc.AccountId,
        SessionId = Truncate(string.IsNullOrWhiteSpace(doc.SessionId) ? "unknown" : doc.SessionId, 100)!,
        EventType = Truncate(doc.EventType ?? string.Empty, 50)!,
        EntityId = Truncate(string.IsNullOrWhiteSpace(doc.EntityId) ? "0" : doc.EntityId, 50)!,
        EntityType = Truncate(doc.EntityType ?? string.Empty, 30)!,
        Source = Truncate(doc.Source, 30),
        Referrer = Truncate(doc.Referrer, 200),
        DeviceType = Truncate(doc.DeviceType, 15),
        DurationMs = doc.DurationMs,
        ScrollDepth = doc.ScrollDepth,
        ClickPosition = Truncate(doc.ClickPosition, 30),
        Metadata = Truncate(doc.Metadata, 500),
        CreatedAt = doc.OccurredAt == default ? doc.CreatedAt : doc.OccurredAt,
    };

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
