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
[Authorize(Roles = "Admin,Staff,Merchandise")]
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
        filter.AssignedAccountId = _currentUserService.AccountId;
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
        var isAdmin = User.IsInRole("Admin");
        var result = await _refundService.UpdateRefundStatusAsync(
            _currentUserService.AccountId, refundId, dto, isAdmin, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<RefundDto>> CreateAdminRefund(
        [FromBody] CreateAdminRefundDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.CreateAdminRefundAsync(_currentUserService.AccountId, dto, cancellationToken);
        return result.ToCreatedResult($"api/admin/refunds/{result.Data?.RefundId}");
    }

    [HttpPost("{refundId:int}/reassign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ReassignRefund(
        [FromRoute] int refundId,
        [FromBody] ToyStore.Application.DTOs.Assignments.ReassignOrderRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.ReassignRefundAsync(refundId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
