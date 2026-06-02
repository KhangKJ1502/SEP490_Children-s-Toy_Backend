using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Accounts;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// APIs quản lý account.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IAccountService accountService, ILogger<AccountsController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách account có phân trang và lọc.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AccountListDto>>> GetAccounts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        [FromQuery] byte? roleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Get accounts called with pageNumber {PageNumber}, pageSize {PageSize}, sortBy {SortBy}, sortDesc {SortDesc}, hasSearchTerm {HasSearchTerm}, roleId {RoleId}",
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            !string.IsNullOrWhiteSpace(searchTerm),
            roleId);

        var result = await _accountService.GetAccountsAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            roleId,
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy chi tiết account theo ID.
    /// </summary>
    [HttpGet("{accountId:int}")]
    public async Task<ActionResult<AccountDto>> GetAccountById(
        [FromRoute] int accountId,
        CancellationToken cancellationToken = default)
    {
        var result = await _accountService.GetAccountByIdAsync(accountId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Tạo mới account Staff hoặc Merchandiser.
    /// </summary>
    [HttpPost]
    // [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AccountDto>> CreateAccount(
        [FromBody] CreateAccountDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _accountService.CreateAccountAsync(dto, cancellationToken);
        return result.ToCreatedResult($"api/accounts/{result.Data?.AccountId}");
    }

    [HttpPut("{accountId:int}")]
    public async Task<ActionResult<AccountDto>> UpdateAccountInfo(
        [FromRoute] int accountId,
        [FromBody] UpdateAccountInfoDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _accountService.UpdateAccountInfoAsync(accountId, dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Cập nhật trạng thái account.
    /// </summary>
    /// <summary>
    /// Admin đổi mật khẩu cho tài khoản Staff/Merchandise.
    /// </summary>
    [HttpPut("{accountId:int}/password")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateAccountPassword(
        [FromRoute] int accountId,
        [FromBody] UpdateAccountPasswordDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _accountService.UpdateAccountPasswordAsync(accountId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
