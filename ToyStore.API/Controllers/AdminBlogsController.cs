using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    private static readonly HashSet<string> AiMeaninglessSingleTokens = new(StringComparer.Ordinal)
    {
        "test",
        "testing",
        "qwerty",
        "qwertyuiop",
        "asdf",
        "asdfghjkl",
        "abcxyz",
    };
    private static readonly HashSet<string> AiMeaninglessPhrases = new(StringComparer.Ordinal)
    {
        "random text",
        "lorem ipsum",
    };
    private static readonly string[] AiKeyboardRows =
    {
        "qwertyuiop",
        "asdfghjkl",
        "zxcvbnm",
    };
    private static readonly HashSet<char> AiVowels = ['a', 'e', 'i', 'o', 'u', 'y'];
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

        var meaninglessErrors = ValidateAiGenerateMeaningfulInputs(request);
        if (meaninglessErrors.Count > 0)
        {
            return BadRequest(new ValidationErrorResponse("One or more validation errors occurred.", meaninglessErrors));
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

    private static Dictionary<string, string[]> ValidateAiGenerateMeaningfulInputs(AiBlogGenerateRequest request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        AddMeaningfulInputError(errors, nameof(request.Title), GetMeaningfulInputError(request.Title, "Title"));
        AddMeaningfulInputError(errors, nameof(request.PromptStructure), GetMeaningfulInputError(request.PromptStructure, "Prompt"));

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            AddMeaningfulInputError(errors, nameof(request.Description), GetMeaningfulInputError(request.Description, "Prompt"));
        }

        return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    private static void AddMeaningfulInputError(
        IDictionary<string, List<string>> errors,
        string key,
        string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (!errors.TryGetValue(key, out var bucket))
        {
            bucket = [];
            errors[key] = bucket;
        }

        bucket.Add(message);
    }

    private static string? GetMeaningfulInputError(string? value, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (!trimmed.Any(char.IsLetterOrDigit))
        {
            return $"{fieldLabel} is invalid.";
        }

        var normalized = NormalizeForMeaningCheck(trimmed);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return $"{fieldLabel} is invalid.";
        }

        if (!normalized.Any(char.IsLetterOrDigit))
        {
            return $"{fieldLabel} is invalid.";
        }

        var compact = normalized.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (!normalized.Any(char.IsLetter) && normalized.Any(char.IsDigit))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        if (compact.Length >= 3 && IsRepeatedSingleCharacter(compact))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        if (compact.Length >= 6 && IsRepeatedPattern(compact))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        if (compact.Length >= 5 && IsKeyboardMash(compact))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        if (IsPlaceholderLike(normalized))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        if (LooksLikeRandomGibberish(normalized))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        return null;
    }

    private static string NormalizeForMeaningCheck(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var normalizedChar = ch == '\u0111' ? 'd' : ch;
            if (char.IsLetterOrDigit(normalizedChar))
            {
                builder.Append(normalizedChar);
            }
            else if (char.IsWhiteSpace(normalizedChar))
            {
                builder.Append(' ');
            }
        }

        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    private static bool IsRepeatedSingleCharacter(string compact)
    {
        return compact.Distinct().Count() == 1;
    }

    private static bool IsRepeatedPattern(string compact)
    {
        for (var size = 1; size <= compact.Length / 2; size++)
        {
            if (compact.Length % size != 0)
            {
                continue;
            }

            var repetitions = compact.Length / size;
            if (repetitions < 3)
            {
                continue;
            }

            var pattern = compact[..size];
            var isRepeated = true;
            for (var index = size; index < compact.Length; index += size)
            {
                if (!compact.AsSpan(index, size).SequenceEqual(pattern))
                {
                    isRepeated = false;
                    break;
                }
            }

            if (isRepeated)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsKeyboardMash(string compact)
    {
        foreach (var row in AiKeyboardRows)
        {
            if (row.Contains(compact, StringComparison.Ordinal) ||
                Reverse(row).Contains(compact, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPlaceholderLike(string normalized)
    {
        if (AiMeaninglessPhrases.Contains(normalized))
        {
            return true;
        }

        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return true;
        }

        if (tokens.Length == 1)
        {
            return AiMeaninglessSingleTokens.Contains(tokens[0]);
        }

        if (tokens.Length >= 2 &&
            tokens.All(token => string.Equals(token, tokens[0], StringComparison.Ordinal)) &&
            AiMeaninglessSingleTokens.Contains(tokens[0]))
        {
            return true;
        }

        return false;
    }

    private static bool LooksLikeRandomGibberish(string normalized)
    {
        var tokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 3 && token.All(char.IsLetter))
            .ToArray();
        if (tokens.Length == 0)
        {
            return false;
        }

        var suspiciousTokens = tokens.Count(IsSuspiciousAlphabeticToken);
        if (suspiciousTokens == 0)
        {
            return false;
        }

        return suspiciousTokens == tokens.Length && tokens.Sum(token => token.Length) >= 10;
    }

    private static bool IsSuspiciousAlphabeticToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 6)
        {
            return false;
        }

        var vowelCount = token.Count(ch => AiVowels.Contains(ch));
        if (vowelCount <= 1)
        {
            return true;
        }

        return LongestConsonantRun(token) >= 5;
    }

    private static int LongestConsonantRun(string token)
    {
        var longest = 0;
        var current = 0;

        foreach (var ch in token)
        {
            if (!char.IsLetter(ch) || AiVowels.Contains(ch))
            {
                current = 0;
                continue;
            }

            current++;
            if (current > longest)
            {
                longest = current;
            }
        }

        return longest;
    }

    private static string Reverse(string value)
    {
        var buffer = value.ToCharArray();
        Array.Reverse(buffer);
        return new string(buffer);
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
