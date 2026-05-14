using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/customer")]
public class CustomerBlogsController : ControllerBase
{
    private readonly IBlogService _blogService;

    public CustomerBlogsController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpGet("blogs")]
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

    [HttpGet("blog-categories")]
    public async Task<ActionResult<List<BlogCategoryDto>>> GetBlogCategories(CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogCategoriesAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("blogs/{blogPostId:int}")]
    public async Task<ActionResult<BlogDetailDto>> GetBlogDetails(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogDetailsAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

}
