using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Products;
using ToyStore.Application.DTOs.Profiles;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/customer/profiles")]
[Authorize(Roles = "Customer")]
public class ProfileCustomerController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IImageUploadService _imageUploadService;

    public ProfileCustomerController(IProfileService profileService, IImageUploadService imageUploadService)
    {
        _profileService = profileService;
        _imageUploadService = imageUploadService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CustomerProfileDto>> GetMyProfile(CancellationToken cancellationToken = default)
    {
        var result = await _profileService.GetMyCustomerProfileAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("me")]
    public async Task<ActionResult<ProfileDto>> UpdateMyProfile(
        [FromBody] UpdateProfileDto dto,
        CancellationToken cancellationToken = default)
    {
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
        var result = await _imageUploadService.UploadImageToFolderAsync(stream, file.FileName, "SEP490_Customers", cancellationToken);
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

    [HttpPut("me/password")]
    public async Task<ActionResult> ChangeMyPassword(
        [FromBody] ChangeCustomerPasswordDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _profileService.ChangeMyCustomerPasswordAsync(dto, cancellationToken);
        return result.ToActionResult();
    }
}
