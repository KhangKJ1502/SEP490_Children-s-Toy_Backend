using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Withdrawals;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/withdrawals")]
[Authorize]
public class WithdrawalsController : ControllerBase
{
    private readonly IWithdrawalService _withdrawalService;

    public WithdrawalsController(IWithdrawalService withdrawalService)
    {
        _withdrawalService = withdrawalService;
    }

    [HttpPost]
    public async Task<ActionResult<WithdrawalDto>> CreateWithdrawal(
        [FromBody] CreateWithdrawalRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _withdrawalService.CreateWithdrawalAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WithdrawalDto>> GetWithdrawal(int id, CancellationToken cancellationToken = default)
    {
        var result = await _withdrawalService.GetWithdrawalAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<WithdrawalDto>>> GetMyWithdrawals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _withdrawalService.GetMyWithdrawalsAsync(page, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult> CancelWithdrawal(int id, CancellationToken cancellationToken = default)
    {
        var result = await _withdrawalService.CancelWithdrawalAsync(id, cancellationToken);
        return result.ToNoContentResult();
    }

}
