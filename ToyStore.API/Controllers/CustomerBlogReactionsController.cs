using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/customer")]
public class CustomerBlogReactionsController : ControllerBase
{
    private readonly IBlogService _blogService;

    public CustomerBlogReactionsController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpPost("blogs/{blogPostId:int}/reactions")]
    [Authorize]
    public async Task<ActionResult<ReactionSummaryDto>> ReactToBlog(
        [FromRoute] int blogPostId,
        [FromBody] UpsertReactionDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.ReactToBlogAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("blogs/{blogPostId:int}/reactions")]
    [Authorize]
    public async Task<ActionResult<bool>> RemoveBlogReaction(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.RemoveBlogReactionAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("blogs/{blogPostId:int}/reactions/summary")]
    public async Task<ActionResult<ReactionSummaryDto>> GetBlogReactionSummary(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogReactionSummaryAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("blogs/{blogPostId:int}/reactions/me")]
    [Authorize]
    public async Task<ActionResult<string?>> GetMyBlogReaction(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetMyBlogReactionAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }
}
