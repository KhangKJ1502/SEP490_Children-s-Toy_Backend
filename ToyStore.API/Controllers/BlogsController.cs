using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

public class UploadThumbnailRequest
{
    public IFormFile File { get; set; } = default!;
}

[ApiController]
[Route("api/[controller]")]
public class BlogsController : ControllerBase
{
    private const string BlogThumbnailFolder = "SEP490_Blogs";
    private readonly IBlogService _blogService;
    private readonly IImageUploadService _imageUploadService;
    private readonly ILogger<BlogsController> _logger;

    public BlogsController(
        IBlogService blogService,
        IImageUploadService imageUploadService,
        ILogger<BlogsController> logger)
    {
        _blogService = blogService;
        _imageUploadService = imageUploadService;
        _logger = logger;
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

    [HttpGet("categories")]
    public async Task<ActionResult<List<BlogCategoryDto>>> GetBlogCategories(CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogCategoriesAsync(cancellationToken);
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
        [FromQuery] bool featuredOnly = false,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] bool? featured = null,
        CancellationToken cancellationToken = default)
    {
        featuredOnly = featuredOnly || isFeatured == true || featured == true;
        var result = await _blogService.GetBlogsForAdminAsync(pageNumber, pageSize, sortBy, sortDesc, searchTerm, status, featuredOnly, cancellationToken);
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
        [FromQuery] bool featuredOnly = false,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] bool? featured = null,
        CancellationToken cancellationToken = default)
    {
        featuredOnly = featuredOnly || isFeatured == true || featured == true;
        var result = await _blogService.GetBlogsForStaffAsync(pageNumber, pageSize, sortBy, sortDesc, searchTerm, status, featuredOnly, cancellationToken);
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

    [HttpGet("{blogPostId:int}/reviews")]
    public async Task<ActionResult<List<BlogReviewDto>>> GetBlogReviews(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogReviewsAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{blogPostId:int}/reviews")]
    [Authorize]
    public async Task<ActionResult<BlogReviewDto>> CreateBlogReview(
        [FromRoute] int blogPostId,
        [FromBody] CreateBlogReviewDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.CreateBlogReviewAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("reviews/{reviewBlogId:int}")]
    [Authorize]
    public async Task<ActionResult<object>> RemoveBlogReview(
        [FromRoute] int reviewBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.RemoveBlogReviewAsync(reviewBlogId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("reviews/{reviewBlogId:int}/replies")]
    [Authorize]
    public async Task<ActionResult<BlogReviewReplyDto>> CreateBlogReviewReply(
        [FromRoute] int reviewBlogId,
        [FromBody] CreateBlogReviewReplyDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.CreateBlogReviewReplyAsync(reviewBlogId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{blogPostId:int}/reactions")]
    [Authorize]
    public async Task<ActionResult<ReactionSummaryDto>> ReactToBlog(
        [FromRoute] int blogPostId,
        [FromBody] UpsertReactionDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.ReactToBlogAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{blogPostId:int}/reactions")]
    [Authorize]
    public async Task<ActionResult<bool>> RemoveBlogReaction(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.RemoveBlogReactionAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{blogPostId:int}/reactions/summary")]
    public async Task<ActionResult<ReactionSummaryDto>> GetBlogReactionSummary(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogReactionSummaryAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{blogPostId:int}/reactions/me")]
    [Authorize]
    public async Task<ActionResult<string?>> GetMyBlogReaction(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetMyBlogReactionAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("reviews/{reviewBlogId:int}/reactions")]
    [Authorize]
    public async Task<ActionResult<ReactionSummaryDto>> ReactToReview(
        [FromRoute] int reviewBlogId,
        [FromBody] UpsertReactionDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.ReactToReviewAsync(reviewBlogId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("reviews/{reviewBlogId:int}/reactions")]
    [Authorize]
    public async Task<ActionResult<bool>> RemoveReviewReaction(
        [FromRoute] int reviewBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.RemoveReviewReactionAsync(reviewBlogId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("reviews/{reviewBlogId:int}/reactions/summary")]
    public async Task<ActionResult<ReactionSummaryDto>> GetReviewReactionSummary(
        [FromRoute] int reviewBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetReviewReactionSummaryAsync(reviewBlogId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("reviews/{reviewBlogId:int}/reactions/me")]
    [Authorize]
    public async Task<ActionResult<string?>> GetMyReviewReaction(
        [FromRoute] int reviewBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetMyReviewReactionAsync(reviewBlogId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("reviews/replies/{replyBlogId:int}/reactions")]
    [Authorize]
    public async Task<ActionResult<ReactionSummaryDto>> ReactToReply(
        [FromRoute] int replyBlogId,
        [FromBody] UpsertReactionDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.ReactToReplyAsync(replyBlogId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("reviews/replies/{replyBlogId:int}/reactions")]
    [Authorize]
    public async Task<ActionResult<bool>> RemoveReplyReaction(
        [FromRoute] int replyBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.RemoveReplyReactionAsync(replyBlogId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("reviews/replies/{replyBlogId:int}/reactions/summary")]
    public async Task<ActionResult<ReactionSummaryDto>> GetReplyReactionSummary(
        [FromRoute] int replyBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetReplyReactionSummaryAsync(replyBlogId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("reviews/replies/{replyBlogId:int}/reactions/me")]
    [Authorize]
    public async Task<ActionResult<string?>> GetMyReplyReaction(
        [FromRoute] int replyBlogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetMyReplyReactionAsync(replyBlogId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("reviews/manage")]
    [Authorize(Roles = "Admin,Staff")]
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

    [HttpPatch("reviews/{reviewBlogId:int}/status")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<BlogReviewDto>> UpdateBlogReviewStatus(
        [FromRoute] int reviewBlogId,
        [FromBody] UpdateBlogReviewStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.UpdateBlogReviewStatusAsync(reviewBlogId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("reviews/replies/{replyBlogId:int}/status")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<BlogReviewReplyDto>> UpdateBlogReplyStatus(
        [FromRoute] int replyBlogId,
        [FromBody] UpdateBlogReviewStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.UpdateBlogReplyStatusAsync(replyBlogId, dto, cancellationToken);
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

    [HttpPatch("{blogPostId:int}/publish-now")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<BlogDetailDto>> PublishNow(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.PublishNowAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("{blogPostId:int}/hide")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<BlogDetailDto>> HideBlog(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.HideBlogAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("{blogPostId:int}/featured")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<BlogDetailDto>> UpdateFeatured(
        [FromRoute] int blogPostId,
        [FromBody] UpdateBlogFeaturedDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.UpdateFeaturedAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("thumbnail/upload")]
    [Authorize(Roles = "Admin,Staff")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<object>> UploadThumbnail(
        [FromForm] UploadThumbnailRequest request,
        CancellationToken cancellationToken = default)
    {
        var file = request.File;
        if (file == null || file.Length <= 0)
        {
            _logger.LogWarning("Upload thumbnail failed: empty file.");
            return BadRequest(new { code = "VALIDATION_ERROR", message = "Thumbnail file is required." });
        }

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
        {
            _logger.LogWarning("Upload thumbnail failed: unsupported extension {Extension}.", extension);
            return BadRequest(new { code = "VALIDATION_ERROR", message = "Only JPG, JPEG, PNG, WEBP, GIF are supported." });
        }

        await using var stream = file.OpenReadStream();
        var uploadResult = await _imageUploadService.UploadImageToFolderAsync(
            stream,
            file.FileName,
            BlogThumbnailFolder,
            cancellationToken);

        if (!uploadResult.IsSuccess)
        {
            _logger.LogWarning(
                "Upload thumbnail failed via Cloudinary. Code: {Code}, Message: {Message}",
                uploadResult.ErrorCode,
                uploadResult.ErrorMessage);

            return BadRequest(new
            {
                code = uploadResult.ErrorCode ?? "UPLOAD_ERROR",
                message = uploadResult.ErrorMessage ?? "Failed to upload thumbnail."
            });
        }

        _logger.LogInformation("Uploaded blog thumbnail to Cloudinary: {Url}", uploadResult.Data);
        return Ok(new { url = uploadResult.Data });
    }
}
