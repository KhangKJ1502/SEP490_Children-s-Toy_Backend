using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Products;
using ToyStore.Application.DTOs.Profiles;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/profiles")]
[Authorize(Roles = "Admin,Staff,Merchandiser,Merchandise")]
public class ProfileAdminController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IImageUploadService _imageUploadService;
    private readonly ILogger<ProfileAdminController> _logger;

    public ProfileAdminController(
        IProfileService profileService,
        IImageUploadService imageUploadService,
        ILogger<ProfileAdminController> logger)
    {
        _profileService = profileService;
        _imageUploadService = imageUploadService;
        _logger = logger;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ProfileDto>> GetMyProfile(CancellationToken cancellationToken = default)
    {
        var result = await _profileService.GetMyProfileAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("me")]
    public async Task<ActionResult<ProfileDto>> UpdateMyProfile(
        [FromBody] UpdateProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Update profile requested by authenticated account.");
        var result = await _profileService.UpdateMyProfileAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("me/avatar")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadImageResponseDto>> UploadImage(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was provided." });
        }

        using var stream = file.OpenReadStream();
        var result = await _imageUploadService.UploadImageAsync(stream, file.FileName, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        var updateProfileResult = await _profileService.UpdateMyProfileAsync(
            new UpdateProfileDto { ImageUrl = result.Data },
            cancellationToken);

        if (!updateProfileResult.IsSuccess)
        {
            return BadRequest(new { message = updateProfileResult.ErrorMessage ?? "Failed to save avatar URL to profile." });
        }

        return Ok(new UploadImageResponseDto { Url = result.Data! });
    }

}
