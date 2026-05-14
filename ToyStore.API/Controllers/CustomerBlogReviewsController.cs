using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/customer")]
public class CustomerBlogReviewsController : ControllerBase
{
    private readonly IBlogService _blogService;

    public CustomerBlogReviewsController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpGet("blogs/{blogPostId:int}/reviews")]
    public async Task<ActionResult<List<BlogReviewDto>>> GetBlogReviews(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogReviewsAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("blogs/{blogPostId:int}/reviews")]
    [Authorize]
    public async Task<ActionResult<BlogReviewDto>> CreateBlogReview(
        [FromRoute] int blogPostId,
        [FromBody] CreateBlogReviewDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.CreateBlogReviewAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("blog-reviews/{reviewBlogId:int}")]
    [Authorize]
    public async Task<ActionResult<object>> RemoveBlogReview(
        [FromRoute] int reviewBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.RemoveBlogReviewAsync(reviewBlogId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("blog-reviews/{reviewBlogId:int}/replies")]
    [Authorize]
    public async Task<ActionResult<BlogReviewReplyDto>> CreateBlogReviewReply(
        [FromRoute] int reviewBlogId,
        [FromBody] CreateBlogReviewReplyDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.CreateBlogReviewReplyAsync(reviewBlogId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
