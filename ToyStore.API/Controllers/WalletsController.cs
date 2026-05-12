using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Wallets;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/wallets")]
[Authorize]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletsController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<WalletDto>> GetMyWallet(CancellationToken cancellationToken = default)
    {
        var result = await _walletService.GetMyWalletAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<PaginatedResponse<WalletTransactionDto>>> GetWalletTransactions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _walletService.GetWalletTransactionsAsync(pageNumber, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("create")]
    public async Task<ActionResult<WalletDto>> CreateWallet(
        [FromBody] CreateWalletRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _walletService.CreateWalletWithPinAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("pin/verify")]
    public async Task<ActionResult<VerifyWalletPinResponseDto>> VerifyPin(
        [FromBody] VerifyWalletPinRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _walletService.VerifyWalletPinAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("pin/change")]
    public async Task<ActionResult> ChangePin(
        [FromBody] ChangeWalletPinRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _walletService.ChangeWalletPinAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("pin/forgot/send-otp")]
    public async Task<ActionResult> SendForgotPinOtp(CancellationToken cancellationToken = default)
    {
        var result = await _walletService.SendForgotWalletPinOtpAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("pin/forgot/verify-otp")]
    public async Task<ActionResult> VerifyForgotPinOtp(
        [FromBody] VerifyForgotWalletPinOtpRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _walletService.VerifyForgotWalletPinOtpAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("pin/forgot/reset")]
    public async Task<ActionResult> ResetForgotPin(
        [FromBody] ResetForgotWalletPinRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _walletService.ResetForgotWalletPinAsync(dto, cancellationToken);
        return result.ToActionResult();
    }
}
