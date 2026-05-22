using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Tracking;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service tiếp nhận batch event từ FE, validate + insert vào MongoDB.session_events.
/// </summary>
public interface ITrackingService
{
    /// <summary>
    /// Ghi nhận batch event của 1 user vào MongoDB (non-blocking pipeline).
    /// </summary>
    Task<Result<TrackEventResponseDto>> TrackEventsAsync(
        TrackEventRequestDto request,
        CancellationToken cancellationToken = default);
}
