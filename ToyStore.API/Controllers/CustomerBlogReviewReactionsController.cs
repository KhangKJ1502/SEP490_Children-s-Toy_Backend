using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/customer")]
public class CustomerBlogReviewReactionsController : ControllerBase
{
    private readonly IBlogService _blogService;

    public CustomerBlogReviewReactionsController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpPost("blog-reviews/{reviewBlogId:int}/reactions")]
    [Authorize]
    public async Task<ActionResult<ReactionSummaryDto>> ReactToReview(
        [FromRoute] int reviewBlogId,
        [FromBody] UpsertReactionDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.ReactToReviewAsync(reviewBlogId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("blog-reviews/{reviewBlogId:int}/reactions")]
    [Authorize]
    public async Task<ActionResult<bool>> RemoveReviewReaction(
        [FromRoute] int reviewBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.RemoveReviewReactionAsync(reviewBlogId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("blog-reviews/{reviewBlogId:int}/reactions/summary")]
    public async Task<ActionResult<ReactionSummaryDto>> GetReviewReactionSummary(
        [FromRoute] int reviewBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetReviewReactionSummaryAsync(reviewBlogId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("blog-reviews/{reviewBlogId:int}/reactions/me")]
    [Authorize]
    public async Task<ActionResult<string?>> GetMyReviewReaction(
        [FromRoute] int reviewBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetMyReviewReactionAsync(reviewBlogId, cancellationToken);
        return result.ToActionResult();
    }
}
