using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlogsController : ControllerBase
{
    private readonly IBlogService _blogService;

    public BlogsController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<PaginatedResponse<BlogListDto>>> SearchBlogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.SearchPublishedBlogsAsync(pageNumber, pageSize, sortBy, sortDesc, searchTerm, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaginatedResponse<BlogListDto>>> GetBlogsForAdmin(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogsForAdminAsync(pageNumber, pageSize, sortBy, sortDesc, searchTerm, status, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("staff")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<PaginatedResponse<BlogListDto>>> GetBlogsForStaff(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogsForStaffAsync(pageNumber, pageSize, sortBy, sortDesc, searchTerm, status, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{blogPostId:int}")]
    public async Task<ActionResult<BlogDetailDto>> GetBlogDetails(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogDetailsAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<BlogDetailDto>> CreateBlog(
        [FromBody] CreateBlogDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.CreateBlogAsync(dto, cancellationToken);
        return result.ToCreatedResult($"/api/blogs/{result.Data?.BlogPostId}");
    }

    [HttpPut("{blogPostId:int}")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<BlogDetailDto>> UpdateBlog(
        [FromRoute] int blogPostId,
        [FromBody] UpdateBlogDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.UpdateBlogAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("{blogPostId:int}/submit")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<BlogDetailDto>> SubmitBlog(
        [FromRoute] int blogPostId,
        [FromBody] SubmitBlogDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.SubmitBlogAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("{blogPostId:int}/approval")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BlogDetailDto>> ApproveBlog(
        [FromRoute] int blogPostId,
        [FromBody] ApproveBlogDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.ApproveBlogAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
