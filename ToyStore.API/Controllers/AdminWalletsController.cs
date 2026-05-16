using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Wallets;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/admin/wallets")]
[Authorize(Roles = "Admin,Staff")]
public class AdminWalletsController : ControllerBase
{
    private readonly IAdminWalletService _adminWalletService;

    public AdminWalletsController(IAdminWalletService adminWalletService)
    {
        _adminWalletService = adminWalletService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AdminWalletListDto>>> GetWallets(
        [FromQuery] AdminWalletQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminWalletService.GetWalletsAsync(query, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("{walletId:int}/status")]
    public async Task<ActionResult<AdminWalletListDto>> UpdateWalletStatus(
        [FromRoute] int walletId,
        [FromBody] UpdateWalletStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminWalletService.UpdateWalletStatusAsync(walletId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
