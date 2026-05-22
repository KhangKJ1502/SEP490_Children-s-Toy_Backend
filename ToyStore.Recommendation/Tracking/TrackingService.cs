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
/// Luồng: Validate → Map sang SessionEventDocument → InsertMany Mongo → trả về số lượng.
/// Toàn bộ pipeline non-blocking: chỉ ghi 1 lần insert nhanh, không gọi sang SQL Server.
/// </summary>
public class TrackingService : ITrackingService
{
    private readonly MongoDbContext _mongo;
    private readonly IValidator<TrackEventRequestDto> _validator;
    private readonly RecommendationOptions _options;
    private readonly ILogger<TrackingService> _logger;

    public TrackingService(
        MongoDbContext mongo,
        IValidator<TrackEventRequestDto> validator,
        IOptions<RecommendationOptions> options,
        ILogger<TrackingService> logger)
    {
        _mongo = mongo;
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

        // 4. Map DTO → MongoDB document
        var now = DateTime.UtcNow;
        var docs = new List<SessionEventDocument>(toInsert.Count);
        foreach (var ev in toInsert)
        {
            docs.Add(new SessionEventDocument
            {
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
                CreatedAt = now,
                FlushedAt = null, // sẽ được FlushEventsJob set khi flush sang SQL Server
            });
        }

        // 5. Bulk insert vào MongoDB (ordered=false → không dừng khi 1 doc lỗi)
        try
        {
            await _mongo.SessionEvents.InsertManyAsync(
                docs,
                new MongoDB.Driver.InsertManyOptions { IsOrdered = false, BypassDocumentValidation = false },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert {Count} events into MongoDB session_events", docs.Count);
            return Result<TrackEventResponseDto>.Failure("TRACKING_INSERT_FAILED", "Failed to persist events.");
        }

        return Result<TrackEventResponseDto>.Success(new TrackEventResponseDto
        {
            AcceptedCount = docs.Count,
            RejectedCount = rejected,
        });
    }
}
