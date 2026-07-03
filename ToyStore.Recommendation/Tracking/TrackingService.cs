using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Tracking;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Recommendation.Configuration;
using ToyStore.Recommendation.MongoDb;
using ToyStore.Recommendation.MongoDb.Documents;

namespace ToyStore.Recommendation.Tracking;

/// <summary>
/// Implementation của ITrackingService.
/// Luồng: Validate → Đẩy TrackEventMessage sang RabbitMQ → trả về số lượng.
/// Toàn bộ pipeline non-blocking: đẩy nhanh vào queue, không gọi thẳng sang SQL Server.
/// </summary>
public class TrackingService : ITrackingService
{
    private readonly ToyStore.Recommendation.RabbitMq.IRabbitMqEventPublisher _rabbitMq;
    private readonly IValidator<TrackEventRequestDto> _validator;
    private readonly RecommendationOptions _options;
    private readonly ILogger<TrackingService> _logger;

    public TrackingService(
        ToyStore.Recommendation.RabbitMq.IRabbitMqEventPublisher rabbitMq,
        IValidator<TrackEventRequestDto> validator,
        IOptions<RecommendationOptions> options,
        ILogger<TrackingService> logger)
    {
        _rabbitMq = rabbitMq;
        _validator = validator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<TrackEventResponseDto>> TrackEventsAsync(
        TrackEventRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate input (FluentValidation)
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result<TrackEventResponseDto>.ValidationFailure(errors);
        }

        // 2. Tracking có thể bị tắt qua config (kill switch khi gặp sự cố Mongo)
        if (!_options.TrackingEnabled)
        {
            _logger.LogWarning("Tracking endpoint is disabled by configuration");
            return Result<TrackEventResponseDto>.Success(new TrackEventResponseDto
            {
                AcceptedCount = 0,
                RejectedCount = request.Events.Count,
            });
        }

        // 3. Giới hạn số event mỗi batch — tránh lạm dụng/DDoS
        var rejected = 0;
        var toInsert = request.Events;
        if (toInsert.Count > _options.MaxEventsPerBatch)
        {
            rejected = toInsert.Count - _options.MaxEventsPerBatch;
            toInsert = toInsert.Take(_options.MaxEventsPerBatch).ToList();
            _logger.LogWarning(
                "Truncated tracking batch from {Original} to {Limit} events for session {SessionId}",
                request.Events.Count, _options.MaxEventsPerBatch, request.SessionId);
        }

        // 4. Map DTO → Message Document
        var now = DateTime.UtcNow;
        var messages = new List<ToyStore.Recommendation.RabbitMq.TrackEventMessage>(toInsert.Count);
        foreach (var ev in toInsert)
        {
            messages.Add(new ToyStore.Recommendation.RabbitMq.TrackEventMessage
            {
                IdempotencyKey = Guid.NewGuid(),
                AccountId = request.AccountId,
                SessionId = request.SessionId,
                EventType = ev.EventType.Trim().ToLowerInvariant(),
                EntityId = ev.EntityId.Trim(),
                EntityType = ev.EntityType.Trim().ToLowerInvariant(),
                Source = ev.Source?.Trim(),
                Referrer = ev.Referrer?.Trim(),
                DeviceType = ev.DeviceType?.Trim().ToLowerInvariant(),
                DurationMs = ev.DurationMs,
                ScrollDepth = ev.ScrollDepth,
                ClickPosition = ev.ClickPosition,
                Metadata = ev.Metadata,
                OccurredAt = ev.OccurredAt ?? now,
                CreatedAt = now
            });
        }

        // 5. Publish to RabbitMQ
        try
        {
            _rabbitMq.PublishEvents("tracking_events", messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {Count} events to RabbitMQ", messages.Count);
            return Result<TrackEventResponseDto>.Failure("TRACKING_PUBLISH_FAILED", "Failed to enqueue events.");
        }

        return Result<TrackEventResponseDto>.Success(new TrackEventResponseDto
        {
            AcceptedCount = messages.Count,
            RejectedCount = rejected,
        });
    }
}
