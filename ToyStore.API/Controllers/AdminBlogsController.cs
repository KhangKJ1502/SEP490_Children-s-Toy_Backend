using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ToyStore.API.Controllers;

public class UploadAdminBlogThumbnailRequest
{
    public IFormFile File { get; set; } = default!;
}

public class AiBlogGenerateRequest
{
    public int? BlogPostId { get; set; }
    public string? Action { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PromptStructure { get; set; } = string.Empty;
    public string? DefaultTone { get; set; }
    public int DefaultCategoryId { get; set; }
    public bool? IsActive { get; set; }
    public string? SourceContent { get; set; }
}

public class AiBlogGenerateResult
{
    public int BlogPostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string BlogContent { get; set; } = string.Empty;
    public int BlogCategoryId { get; set; }
    public string PromptData { get; set; } = string.Empty;
    public string AiStatus { get; set; } = "Success";
    public string? AiError { get; set; }
}

internal sealed class PythonBlogGenerateRequest
{
    public string Action { get; set; } = "Generate";
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PromptStructure { get; set; } = string.Empty;
    public string DefaultTone { get; set; } = "Friendly";
    public int DefaultCategoryId { get; set; }
    public string? SourceContent { get; set; }
}

internal sealed class PythonBlogGenerateResponse
{
    public string? Status { get; set; }
    public string? Violation_Type { get; set; }
    public string? Violated_Keyword { get; set; }
    public string? Reason { get; set; }
    public string[]? Suggestions { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin,Staff")]
public class AdminBlogsController : ControllerBase
{
    private const string BlogThumbnailFolder = "SEP490_Blogs";
    private const string AiModerationHttpClientName = "AI_MODERATION";
    private readonly IBlogService _blogService;
    private readonly IImageUploadService _imageUploadService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AiModerationOptions> _aiModerationOptions;
    private readonly ILogger<AdminBlogsController> _logger;

    public AdminBlogsController(
        IBlogService blogService,
        IImageUploadService imageUploadService,
        IHttpClientFactory httpClientFactory,
        IOptions<AiModerationOptions> aiModerationOptions,
        ILogger<AdminBlogsController> logger)
    {
        _blogService = blogService;
        _imageUploadService = imageUploadService;
        _httpClientFactory = httpClientFactory;
        _aiModerationOptions = aiModerationOptions;
        _logger = logger;
    }

    [HttpGet("blogs")]
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

    [HttpGet("blogs/my")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<PaginatedResponse<BlogListDto>>> GetMyBlogsForStaff(
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

    [HttpGet("blogs/{blogPostId:int}")]
    public async Task<ActionResult<BlogDetailDto>> GetBlogDetails(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogDetailsAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("blog-categories")]
    public async Task<ActionResult<List<BlogCategoryDto>>> GetBlogCategories(CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogCategoriesAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("blogs")]
    public async Task<ActionResult<BlogDetailDto>> CreateBlog(
        [FromBody] CreateBlogDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.CreateBlogAsync(dto, cancellationToken);
        return result.ToCreatedResult($"/api/admin/blogs/{result.Data?.BlogPostId}");
    }

    [HttpPut("blogs/{blogPostId:int}")]
    public async Task<ActionResult<BlogDetailDto>> UpdateBlog(
        [FromRoute] int blogPostId,
        [FromBody] UpdateBlogDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.UpdateBlogAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("blogs/{blogPostId:int}/submit")]
    public async Task<ActionResult<BlogDetailDto>> SubmitBlog(
        [FromRoute] int blogPostId,
        [FromBody] SubmitBlogDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.SubmitBlogAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("blogs/{blogPostId:int}/approval")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BlogDetailDto>> ApproveBlog(
        [FromRoute] int blogPostId,
        [FromBody] ApproveBlogDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.ApproveBlogAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("blogs/{blogPostId:int}/publish-now")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BlogDetailDto>> PublishNow(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.PublishNowAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("blogs/{blogPostId:int}/hide")]
    public async Task<ActionResult<BlogDetailDto>> HideBlog(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.HideBlogAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("blogs/{blogPostId:int}/featured")]
    public async Task<ActionResult<BlogDetailDto>> UpdateFeatured(
        [FromRoute] int blogPostId,
        [FromBody] UpdateBlogFeaturedDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.UpdateFeaturedAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("ai-blogs/generate")]
    public async Task<ActionResult<AiBlogGenerateResult>> GenerateBlogWithAi(
        [FromBody] AiBlogGenerateRequest request,
        CancellationToken cancellationToken = default)
    {
        static ActionResult<AiBlogGenerateResult> ToAiErrorResult(string code, string message, int statusCode = 400)
            => new ObjectResult(new { code, message }) { StatusCode = statusCode };

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.PromptStructure) || request.DefaultCategoryId <= 0)
        {
            return BadRequest(new { code = "VALIDATION_ERROR", message = "Title, PromptStructure, and Category are required." });
        }
        if (request.DefaultCategoryId > short.MaxValue)
        {
            return BadRequest(new { code = "VALIDATION_ERROR", message = "Category is invalid." });
        }

        var action = string.IsNullOrWhiteSpace(request.Action) ? "Generate" : request.Action.Trim();
        var tone = string.IsNullOrWhiteSpace(request.DefaultTone) ? "Friendly" : request.DefaultTone.Trim();

        string? sourceContent = string.IsNullOrWhiteSpace(request.SourceContent) ? null : request.SourceContent.Trim();
        if (request.BlogPostId.HasValue && request.BlogPostId.Value > 0)
        {
            if (string.IsNullOrWhiteSpace(sourceContent))
            {
                var existingResult = await _blogService.GetBlogDetailsAsync(request.BlogPostId.Value, cancellationToken);
                if (!existingResult.IsSuccess || existingResult.Data == null)
                {
                    return ToAiErrorResult(existingResult.ErrorCode ?? "BLOG_NOT_FOUND", existingResult.ErrorMessage ?? "Blog not found.", 404);
                }
                sourceContent = existingResult.Data.BlogContent;
            }
        }

        PythonBlogGenerateResponse? aiGenerated;
        try
        {
            using var client = _httpClientFactory.CreateClient(AiModerationHttpClientName);
            using var aiRequest = new HttpRequestMessage(HttpMethod.Post, "/moderation/blog-content/generate")
            {
                Content = JsonContent.Create(new PythonBlogGenerateRequest
                {
                    Action = action,
                    Title = request.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                    PromptStructure = request.PromptStructure.Trim(),
                    DefaultTone = tone,
                    DefaultCategoryId = request.DefaultCategoryId,
                    SourceContent = sourceContent
                })
            };
            aiRequest.Headers.Add("X-Internal-Key", _aiModerationOptions.Value.InternalApiKey);

            using var aiResponse = await client.SendAsync(aiRequest, cancellationToken);
            if (!aiResponse.IsSuccessStatusCode)
            {
                var aiErrorBody = await aiResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("AI blog generate failed. Status={StatusCode}, Body={Body}", aiResponse.StatusCode, aiErrorBody);
                return StatusCode((int)aiResponse.StatusCode, new { code = "AI_GENERATE_ERROR", message = "AI generation failed.", detail = aiErrorBody });
            }

            await using var aiStream = await aiResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var aiJsonDoc = await JsonDocument.ParseAsync(aiStream, cancellationToken: cancellationToken);
            var root = aiJsonDoc.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("status", out var statusNode)
                && string.Equals(statusNode.GetString(), "blocked", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(root.Clone());
            }
            var content = root.TryGetProperty("content", out var contentNode) ? contentNode.GetString() : null;
            var titleFromAi = root.TryGetProperty("title", out var titleNode) ? titleNode.GetString() : null;
            aiGenerated = new PythonBlogGenerateResponse
            {
                Title = titleFromAi ?? string.Empty,
                Content = content ?? string.Empty,
            };
            if (aiGenerated == null || string.IsNullOrWhiteSpace(aiGenerated.Content))
            {
                return StatusCode(502, new { code = "AI_GENERATE_EMPTY", message = "AI returned empty content." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI blog generate call threw exception.");
            return StatusCode(502, new { code = "AI_SERVICE_UNAVAILABLE", message = "Unable to call AI service." });
        }

        var generatedTitle = string.IsNullOrWhiteSpace(aiGenerated.Title)
            ? request.Title.Trim()
            : aiGenerated.Title.Trim();

        return Ok(new AiBlogGenerateResult
        {
            BlogPostId = request.BlogPostId.GetValueOrDefault(0),
            Title = generatedTitle,
            BlogContent = aiGenerated.Content,
            BlogCategoryId = request.DefaultCategoryId,
            PromptData = request.PromptStructure,
            AiStatus = "Success",
            AiError = null
        });
    }

    [HttpPost("blogs/thumbnail/upload")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<object>> UploadThumbnail(
        [FromForm] UploadAdminBlogThumbnailRequest request,
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
            ".jpg", ".jpeg", ".png", ".webp"
        };

        var allowedMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
        {
            _logger.LogWarning("Upload thumbnail failed: unsupported extension {Extension}.", extension);
            return BadRequest(new { code = "VALIDATION_ERROR", message = "Only JPG, JPEG, PNG, WEBP are supported." });
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !allowedMimeTypes.Contains(file.ContentType))
        {
            _logger.LogWarning("Upload thumbnail failed: unsupported mime type {MimeType}.", file.ContentType);
            return BadRequest(new { code = "VALIDATION_ERROR", message = "Only image/jpeg, image/png, image/webp are supported." });
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
