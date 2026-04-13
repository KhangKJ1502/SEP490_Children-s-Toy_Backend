using Microsoft.AspNetCore.Mvc;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    /// Gets personalized recommendations for a user.
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RecommendationDto>>>> GetPersonalized(
        Guid userId,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var recommendations = await _recommendationService.GetPersonalizedRecommendationsAsync(
            userId, limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RecommendationDto>>.Ok(recommendations));
    }
    
    /// <summary>
    /// Gets recommendations based on criteria.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RecommendationDto>>>> GetRecommendations(
        [FromBody] GetRecommendationsDto request,
        CancellationToken cancellationToken)
    {
        var recommendations = await _recommendationService.GetRecommendationsAsync(
            request, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RecommendationDto>>.Ok(recommendations));
    }
    
    /// <summary>
    /// Gets similar products.
    /// </summary>
    [HttpGet("similar/{productId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RecommendationDto>>>> GetSimilar(
        Guid productId,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var recommendations = await _recommendationService.GetSimilarProductsAsync(
            productId, limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RecommendationDto>>.Ok(recommendations));
    }
    
    /// <summary>
    /// Gets frequently bought together products.
    /// </summary>
    [HttpGet("bought-together/{productId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RecommendationDto>>>> GetBoughtTogether(
        Guid productId,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var recommendations = await _recommendationService.GetFrequentlyBoughtTogetherAsync(
            productId, limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RecommendationDto>>.Ok(recommendations));
    }
    
    /// <summary>
    /// Tracks user behavior (view, add to cart, etc.).
    /// </summary>
    [HttpPost("track")]
    public async Task<ActionResult<ApiResponse>> TrackBehavior(
        [FromBody] TrackBehaviorDto behavior,
        CancellationToken cancellationToken)
    {
        await _recommendationService.TrackBehaviorAsync(behavior, cancellationToken);
        return Ok(ApiResponse.Ok("Behavior tracked successfully"));
    }
}
