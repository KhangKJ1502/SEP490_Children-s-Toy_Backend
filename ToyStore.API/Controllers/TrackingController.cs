using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Tracking;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// Endpoint nhận batch event tracking từ FE (gom 10 events / 30 giây / khi rời trang).
/// API non-blocking: validate → ghi MongoDB → return ngay.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Cho phép cả guest gửi event (chưa login)
public class TrackingController : ControllerBase
{
    private readonly ITrackingService _trackingService;
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(
        ITrackingService trackingService,
        ILogger<TrackingController> logger)
    {
        _trackingService = trackingService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/tracking — nhận batch event từ FE và buffer vào MongoDB.session_events.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TrackEventResponseDto>> TrackEvents(
        [FromBody] TrackEventRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _trackingService.TrackEventsAsync(request, cancellationToken);
        // Khi không có lỗi -> trả 202 Accepted (đã nhận, đang xử lý bất đồng bộ)
        if (result.IsSuccess)
        {
            return Accepted(result.Data);
        }
        return result.ToActionResult();
    }

    /// <summary>
    /// Endpoint cho navigator.sendBeacon (Content-Type có thể là text/plain) — FE gọi khi đóng tab.
    /// Body JSON giống TrackEventRequestDto nhưng ASP.NET không tự bind nếu thiếu content-type;
    /// vì vậy nhận raw stream rồi tự deserialize.
    /// </summary>
    [HttpPost("beacon")]
    public async Task<IActionResult> TrackEventsBeacon(CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
                return BadRequest();

            var request = System.Text.Json.JsonSerializer.Deserialize<TrackEventRequestDto>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null)
                return BadRequest();

            var result = await _trackingService.TrackEventsAsync(request, cancellationToken);
            return result.IsSuccess ? Accepted() : BadRequest();
        }
        catch (Exception ex)
        {
            // Beacon là fire-and-forget — log cảnh báo nhưng không ném exception ra ngoài
            _logger.LogWarning(ex, "Tracking beacon failed to process");
            return BadRequest();
        }
    }
}
