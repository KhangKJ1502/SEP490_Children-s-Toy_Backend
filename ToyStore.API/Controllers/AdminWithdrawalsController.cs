using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Withdrawals;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// Controller xử lý các yêu cầu quản lý rút tiền dành cho Admin.
/// </summary>
[ApiController]
[Route("api/admin/withdrawals")]
[Authorize(Roles = "Admin")]
public class AdminWithdrawalsController : ControllerBase
{
    private readonly IWithdrawalService _withdrawalService;

    public AdminWithdrawalsController(IWithdrawalService withdrawalService)
    {
        _withdrawalService = withdrawalService;
    }

    /// <summary>
    /// Lấy danh sách lịch sử yêu cầu rút tiền phân trang và lọc cho Admin.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AdminWithdrawalListDto>>> GetAdminWithdrawals(
        [FromQuery] AdminWithdrawalFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var result = await _withdrawalService.GetAdminWithdrawalsAsync(filter, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy chi tiết yêu cầu rút tiền kèm lịch sử xử lý.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminWithdrawalDetailDto>> GetAdminWithdrawalById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _withdrawalService.AdminGetWithdrawalByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
