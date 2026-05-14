using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin,Staff")]
public class AdminBlogReviewsController : ControllerBase
{
    private readonly IBlogService _blogService;

    public AdminBlogReviewsController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpGet("blog-reviews")]
    public async Task<ActionResult<PaginatedResponse<BlogReviewDto>>> GetBlogReviewsForManagement(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogReviewsForManagementAsync(pageNumber, pageSize, searchTerm, status, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("blog-reviews/{reviewBlogId:int}/status")]
    public async Task<ActionResult<BlogReviewDto>> UpdateBlogReviewStatus(
        [FromRoute] int reviewBlogId,
        [FromBody] UpdateBlogReviewStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.UpdateBlogReviewStatusAsync(reviewBlogId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("blog-review-replies/{replyBlogId:int}/status")]
    public async Task<ActionResult<BlogReviewReplyDto>> UpdateBlogReplyStatus(
        [FromRoute] int replyBlogId,
        [FromBody] UpdateBlogReviewStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.UpdateBlogReplyStatusAsync(replyBlogId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("blog-reviews/{reviewBlogId:int}/replies")]
    public async Task<ActionResult<BlogReviewReplyDto>> CreateBlogReviewReply(
        [FromRoute] int reviewBlogId,
        [FromBody] CreateBlogReviewReplyDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.CreateBlogReviewReplyAsync(reviewBlogId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
