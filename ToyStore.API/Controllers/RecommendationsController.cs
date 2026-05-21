using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Recommendations;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// Endpoint phục vụ widget gợi ý cho FE.
/// Cho phép truy cập không login (guest) — vì có widget homepage_trending dùng cho cả guest.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(
        IRecommendationService recommendationService,
        ILogger<RecommendationsController> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/recommendations?widgetCode=homepage_trending&productId=12&accountId=5
    /// AccountId có thể được resolve từ JWT nếu user đã login — frontend không cần gửi.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<RecommendationWidgetResponseDto>> GetRecommendations(
        [FromQuery] string widgetCode,
        [FromQuery] int? productId,
        [FromQuery] int? accountId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(widgetCode))
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", "widgetCode is required."));

        // Ưu tiên accountId từ JWT (nếu user login), nếu không thì dùng query
        var resolvedAccountId = ResolveAccountIdFromClaims() ?? accountId;

        var result = await _recommendationService.GetRecommendationsAsync(
            widgetCode.Trim(),
            resolvedAccountId,
            productId,
            cancellationToken);

        return result.ToActionResult();
    }

    private int? ResolveAccountIdFromClaims()
    {
        if (User?.Identity?.IsAuthenticated != true) return null;
        var claim = User.FindFirst("AccountId")?.Value
                  ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) && id > 0 ? id : null;
    }
}
