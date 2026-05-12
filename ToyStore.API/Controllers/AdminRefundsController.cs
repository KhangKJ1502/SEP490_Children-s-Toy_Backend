using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/admin/refunds")]
[Authorize(Roles = "Admin,Staff")]
public class AdminRefundsController : ControllerBase
{
    private readonly IRefundService _refundService;
    private readonly ICurrentUserService _currentUserService;

    public AdminRefundsController(IRefundService refundService, ICurrentUserService currentUserService)
    {
        _refundService = refundService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RefundListDto>>> GetAdminRefunds(
        [FromQuery] AdminRefundFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.GetAdminRefundsAsync(filter, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<RefundListDto>>.Ok(result));
    }

    [HttpGet("{refundId:int}")]
    public async Task<ActionResult<RefundDto>> GetAdminRefundById(
        [FromRoute] int refundId,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.AdminGetRefundByIdAsync(refundId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("{refundId:int}/status")]
    public async Task<ActionResult<RefundDto>> UpdateRefundStatus(
        [FromRoute] int refundId,
        [FromBody] UpdateRefundStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.UpdateRefundStatusAsync(_currentUserService.AccountId, refundId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
