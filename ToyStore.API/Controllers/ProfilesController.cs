using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Profiles;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Staff,Merchandiser,Merchandise")]
public class ProfilesController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfilesController> _logger;

    public ProfilesController(IProfileService profileService, ILogger<ProfilesController> logger)
    {
        _profileService = profileService;
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

}
