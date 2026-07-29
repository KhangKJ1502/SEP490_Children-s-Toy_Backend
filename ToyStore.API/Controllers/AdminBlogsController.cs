using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators.Blogs;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin,Staff")]
public class AdminBlogsController : ControllerBase
{
    private readonly IBlogService _blogService;
    private readonly ILogger<AdminBlogsController> _logger;

    public AdminBlogsController(
        IBlogService blogService,
        ILogger<AdminBlogsController> logger)
    {
        _blogService = blogService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách bài blog cho Admin (có phân trang, tìm kiếm, lọc).
    /// </summary>
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
        // Hỗ trợ nhiều alias query param cho featuredOnly
        featuredOnly = featuredOnly || isFeatured == true || featured == true;
        var result = await _blogService.GetBlogsForAdminAsync(pageNumber, pageSize, sortBy, sortDesc, searchTerm, status, featuredOnly, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy danh sách bài blog của Staff hiện tại (có phân trang, tìm kiếm, lọc).
    /// </summary>
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

    /// <summary>
    /// Lấy chi tiết bài blog theo ID.
    /// </summary>
    [HttpGet("blogs/{blogPostId:int}")]
    public async Task<ActionResult<BlogDetailDto>> GetBlogDetails(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogDetailsAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy danh sách danh mục blog.
    /// </summary>
    [HttpGet("blog-categories")]
    public async Task<ActionResult<List<BlogCategoryDto>>> GetBlogCategories(CancellationToken cancellationToken = default)
    {
        var result = await _blogService.GetBlogCategoriesAsync(cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Tạo bài blog mới.
    /// </summary>
    [HttpPost("blogs")]
    public async Task<ActionResult<BlogDetailDto>> CreateBlog(
        [FromBody] CreateBlogDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.CreateBlogAsync(dto, cancellationToken);
        return result.ToCreatedResult($"/api/admin/blogs/{result.Data?.BlogPostId}");
    }

    /// <summary>
    /// Cập nhật bài blog theo ID.
    /// </summary>
    [HttpPut("blogs/{blogPostId:int}")]
    public async Task<ActionResult<BlogDetailDto>> UpdateBlog(
        [FromRoute] int blogPostId,
        [FromBody] UpdateBlogDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.UpdateBlogAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Staff nộp bài blog để duyệt.
    /// </summary>
    [HttpPatch("blogs/{blogPostId:int}/submit")]
    public async Task<ActionResult<BlogDetailDto>> SubmitBlog(
        [FromRoute] int blogPostId,
        [FromBody] SubmitBlogDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.SubmitBlogAsync(blogPostId, dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Admin duyệt hoặc từ chối bài blog.
    /// </summary>
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

    /// <summary>
    /// Admin publish ngay lập tức bài blog đã được duyệt.
    /// </summary>
    [HttpPatch("blogs/{blogPostId:int}/publish-now")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BlogDetailDto>> PublishNow(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.PublishNowAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Ẩn bài blog đang được public.
    /// </summary>
    [HttpPatch("blogs/{blogPostId:int}/hide")]
    public async Task<ActionResult<BlogDetailDto>> HideBlog(
        [FromRoute] int blogPostId,
        CancellationToken cancellationToken = default)
    {
        var result = await _blogService.HideBlogAsync(blogPostId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Generate nội dung bài blog bằng AI.
    /// Toàn bộ validation và logic nằm trong BlogService.
    /// </summary>
    [HttpPost("ai-blogs/generate")]
    public async Task<ActionResult<AiBlogGenerateResult>> GenerateBlogWithAi(
        [FromBody] AiBlogGenerateRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate cơ bản bằng FluentValidation trước khi vào service
        var validator = new AiBlogGenerateRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var fieldErrors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return BadRequest(new ValidationErrorResponse("One or more validation errors occurred.", fieldErrors));
        }

        var result = await _blogService.GenerateWithAiAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Upload thumbnail cho bài blog lên Cloudinary.
    /// Toàn bộ validation (extension, MIME, size) và upload nằm trong BlogService.
    /// </summary>
    [HttpPost("blogs/thumbnail/upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<UploadBlogThumbnailResponse>> UploadThumbnail(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        // Guard đơn giản: file tồn tại — đây là input binding check, không phải business logic
        if (file == null || file.Length <= 0)
        {
            _logger.LogWarning("Upload thumbnail: empty file received.");
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", "Thumbnail file is required."));
        }

        await using var stream = file.OpenReadStream();
        var result = await _blogService.UploadBlogThumbnailAsync(
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        return result.ToActionResult();
    }
}
