using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Services;

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

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin,Staff")]
public class AdminBlogsController : ControllerBase
{
    private const string BlogThumbnailFolder = "SEP490_Blogs";
    private static readonly string[] AiKeyboardRows =
    {
        "qwertyuiop",
        "asdfghjkl",
        "zxcvbnm",
    };
    private static readonly HashSet<char> AiVowels = ['a', 'e', 'i', 'o', 'u', 'y'];
    private static readonly IReadOnlyDictionary<char, (double Column, int Row)> AiKeyboardCoordinates = BuildKeyboardCoordinates();
    private readonly IBlogService _blogService;
    private readonly IImageUploadService _imageUploadService;
    private readonly IBlogContentGenerationGateway _blogContentGenerationGateway;
    private readonly ILogger<AdminBlogsController> _logger;

    public AdminBlogsController(
        IBlogService blogService,
        IImageUploadService imageUploadService,
        IBlogContentGenerationGateway blogContentGenerationGateway,
        ILogger<AdminBlogsController> logger)
    {
        _blogService = blogService;
        _imageUploadService = imageUploadService;
        _blogContentGenerationGateway = blogContentGenerationGateway;
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
        if (request.DefaultCategoryId <= 0 || request.DefaultCategoryId > short.MaxValue)
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

        var aiResult = await _blogContentGenerationGateway.GenerateAsync(
            new PythonBlogGenerateRequest
            {
                Action = action,
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                PromptStructure = request.PromptStructure.Trim(),
                DefaultTone = tone,
                DefaultCategoryId = request.DefaultCategoryId,
                SourceContent = sourceContent
            },
            cancellationToken);

        if (!aiResult.IsSuccess)
        {
            if (aiResult.ErrorBody != null)
            {
                return StatusCode(aiResult.StatusCode ?? 502, new { code = "AI_GENERATE_ERROR", message = "AI generation failed.", detail = aiResult.ErrorBody });
            }

            return StatusCode(502, new { code = "AI_SERVICE_UNAVAILABLE", message = aiResult.ErrorMessage ?? "Unable to call AI service." });
        }

        var aiGenerated = aiResult.Data;
        if (aiGenerated?.IsBlocked == true)
        {
            return Ok(aiGenerated);
        }

        if (aiGenerated == null || string.IsNullOrWhiteSpace(aiGenerated.Content))
        {
            return StatusCode(502, new { code = "AI_GENERATE_EMPTY", message = "AI returned empty content." });
        }

        var generatedTitle = string.IsNullOrWhiteSpace(aiGenerated.Title)
            ? request.Title.Trim()
            : aiGenerated.Title.Trim();

        return Ok(new AiBlogGenerateResult
        {
            BlogPostId = request.BlogPostId.GetValueOrDefault(0),
            Title = generatedTitle,
            BlogContent = aiGenerated.Content!,
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

        if (compact.Length < 3 && normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length == 1)
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        if (compact.Length >= 2 && IsRepeatedSingleCharacter(compact))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        if (compact.Length >= 4 && IsRepeatedPattern(compact))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        if (IsNearRepeatedPattern(compact))
        {
            return $"{fieldLabel} appears to be meaningless.";
        }

        if (IsKeyboardMash(compact))
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
            if (repetitions < 2)
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

    private static bool IsNearRepeatedPattern(string compact)
    {
        if (compact.Length < 6)
        {
            return false;
        }

        if (IsRepeatedPattern(compact))
        {
            return true;
        }

        for (var index = 0; index < compact.Length; index++)
        {
            var withoutOneChar = compact.Remove(index, 1);
            if (withoutOneChar.Length >= 6 && IsRepeatedPattern(withoutOneChar))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsKeyboardMash(string compact)
    {
        if (compact.Length < 3 || compact.Any(ch => ch < 'a' || ch > 'z'))
        {
            return false;
        }

        foreach (var row in AiKeyboardRows)
        {
            if (row.Contains(compact, StringComparison.Ordinal) ||
                Reverse(row).Contains(compact, StringComparison.Ordinal))
            {
                return true;
            }
        }

        if (compact.Length > 8 || !IsKeyboardWalk(compact))
        {
            return false;
        }

        var vowelRatio = GetVowelRatio(compact);
        return LongestConsonantRun(compact) >= 2 ||
            vowelRatio < 0.3 ||
            GetUniqueLetterRatio(compact) <= 0.6;
    }

    private static bool IsKeyboardChunk(string value)
    {
        if (value.Length < 2)
        {
            return false;
        }

        foreach (var row in AiKeyboardRows)
        {
            if (row.Contains(value, StringComparison.Ordinal) ||
                Reverse(row).Contains(value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsKeyboardWalk(string compact)
    {
        for (var index = 1; index < compact.Length; index++)
        {
            if (!AiKeyboardCoordinates.TryGetValue(compact[index - 1], out var previous) ||
                !AiKeyboardCoordinates.TryGetValue(compact[index], out var current))
            {
                return false;
            }

            var horizontalDistance = Math.Abs(previous.Column - current.Column);
            var verticalDistance = Math.Abs(previous.Row - current.Row);
            if (horizontalDistance > 1.5 || verticalDistance > 1)
            {
                return false;
            }
        }

        return true;
    }

    private static double GetVowelRatio(string token)
    {
        if (token.Length == 0)
        {
            return 0;
        }

        var vowelCount = token.Count(ch => AiVowels.Contains(ch));
        return (double)vowelCount / token.Length;
    }

    private static double GetUniqueLetterRatio(string token)
    {
        return token.Length == 0 ? 0 : (double)token.Distinct().Count() / token.Length;
    }

    private static bool HasRepeatedKeyboardChunk(string token)
    {
        if (token.Length < 6)
        {
            return false;
        }

        for (var size = 3; size <= Math.Min(4, token.Length / 2); size++)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index <= token.Length - size; index++)
            {
                var chunk = token.Substring(index, size);
                if (!IsKeyboardChunk(chunk))
                {
                    continue;
                }

                counts[chunk] = counts.TryGetValue(chunk, out var count) ? count + 1 : 1;
                if (counts[chunk] >= 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasLowDiversityNoiseShape(string token)
    {
        if (token.Length < 7)
        {
            return false;
        }

        var uniqueLetters = token.Distinct().Count();
        if (uniqueLetters > 3)
        {
            return false;
        }

        var bigrams = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < token.Length - 1; index++)
        {
            bigrams.Add(token.Substring(index, 2));
        }

        return (double)bigrams.Count / (token.Length - 1) <= 0.55;
    }

    private static bool LooksLikeRandomGibberish(string normalized)
    {
        var rawTokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        if (rawTokens.Any(IsSuspiciousMixedAlphanumericToken))
        {
            return true;
        }

        var tokens = rawTokens
            .Where(token => token.Length >= 3 && token.All(char.IsLetter))
            .ToArray();
        if (tokens.Length == 0)
        {
            return false;
        }

        var suspiciousTokens = tokens.Count(token => IsSuspiciousAlphabeticToken(token) || IsLowVowelNoiseToken(token));
        if (suspiciousTokens == 0)
        {
            return false;
        }

        return suspiciousTokens == tokens.Length;
    }

    private static bool IsSuspiciousMixedAlphanumericToken(string token)
    {
        if (!token.Any(char.IsLetter) || !token.Any(char.IsDigit))
        {
            return false;
        }

        var lettersOnly = new string(token.Where(char.IsLetter).ToArray());
        if (lettersOnly.Length < 3)
        {
            return true;
        }

        var digitGroups = Regex.Matches(token, @"\d+").Count;
        var letterSegments = Regex.Split(token, @"\d+").Where(segment => !string.IsNullOrWhiteSpace(segment)).ToArray();
        if (letterSegments.Length >= 2)
        {
            return true;
        }

        return IsSuspiciousAlphabeticToken(lettersOnly)
            || IsLowVowelNoiseToken(lettersOnly)
            || digitGroups >= 2;
    }

    private static bool IsSuspiciousAlphabeticToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 3)
        {
            return false;
        }

        if (IsKeyboardMash(token) || IsNearRepeatedPattern(token) || HasRepeatedKeyboardChunk(token))
        {
            return true;
        }

        var longestConsonants = LongestConsonantRun(token);
        var vowelCount = token.Count(ch => AiVowels.Contains(ch));
        if (token.Length <= 4)
        {
            return vowelCount == 0 || longestConsonants >= token.Length;
        }

        if (vowelCount == 0)
        {
            return true;
        }

        if (vowelCount <= 1)
        {
            return true;
        }

        if (longestConsonants >= 4)
        {
            return true;
        }

        if (GetVowelRatio(token) < 0.25)
        {
            return true;
        }

        return HasLowDiversityNoiseShape(token);
    }

    private static bool IsLowVowelNoiseToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 5)
        {
            return false;
        }

        var vowelRatio = GetVowelRatio(token);
        if (vowelRatio > 0.35)
        {
            return false;
        }

        return LongestConsonantRun(token) >= 3 || GetUniqueLetterRatio(token) <= 0.45;
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

    private static IReadOnlyDictionary<char, (double Column, int Row)> BuildKeyboardCoordinates()
    {
        var coordinates = new Dictionary<char, (double Column, int Row)>();
        for (var rowIndex = 0; rowIndex < AiKeyboardRows.Length; rowIndex++)
        {
            var rowOffset = rowIndex * 0.5;
            var row = AiKeyboardRows[rowIndex];
            for (var columnIndex = 0; columnIndex < row.Length; columnIndex++)
            {
                coordinates[row[columnIndex]] = (columnIndex + rowOffset, rowIndex);
            }
        }

        return coordinates;
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
