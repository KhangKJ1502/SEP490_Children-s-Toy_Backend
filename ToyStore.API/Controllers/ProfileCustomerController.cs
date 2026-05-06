using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Profiles;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/customer/profiles")]
[Authorize(Roles = "Customer")]
public class ProfileCustomerController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileCustomerController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CustomerProfileDto>> GetMyProfile(CancellationToken cancellationToken = default)
    {
        var result = await _profileService.GetMyCustomerProfileAsync(cancellationToken);
        return result.ToActionResult();
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
