using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/customer")]
public class CustomerBlogReviewReplyReactionsController : ControllerBase
{
    private readonly IBlogService _blogService;

    public CustomerBlogReviewReplyReactionsController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpPost("blog-review-replies/{replyBlogId:int}/reactions")]
    [Authorize]
    public async Task<ActionResult<ReactionSummaryDto>> ReactToReply(
        [FromRoute] int replyBlogId,
        [FromBody] UpsertReactionDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.ReactToReplyAsync(replyBlogId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("blog-review-replies/{replyBlogId:int}/reactions")]
    [Authorize]
    public async Task<ActionResult<bool>> RemoveReplyReaction(
        [FromRoute] int replyBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.RemoveReplyReactionAsync(replyBlogId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("blog-review-replies/{replyBlogId:int}/reactions/summary")]
    public async Task<ActionResult<ReactionSummaryDto>> GetReplyReactionSummary(
        [FromRoute] int replyBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetReplyReactionSummaryAsync(replyBlogId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("blog-review-replies/{replyBlogId:int}/reactions/me")]
    [Authorize]
    public async Task<ActionResult<string?>> GetMyReplyReaction(
        [FromRoute] int replyBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetMyReplyReactionAsync(replyBlogId, cancellationToken);
        return result.ToActionResult();
    }
}
