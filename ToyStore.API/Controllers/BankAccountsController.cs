using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.BankAccounts;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/bank-accounts")]
[Authorize]
public class BankAccountsController : ControllerBase
{
    private readonly ISavedBankAccountService _bankAccountService;
    private readonly IBankLookupService _bankLookupService;

    public BankAccountsController(
        ISavedBankAccountService bankAccountService,
        IBankLookupService bankLookupService)
    {
        _bankAccountService = bankAccountService;
        _bankLookupService = bankLookupService;
    }

    [HttpGet]
    public async Task<ActionResult<List<SavedBankAccountDto>>> GetMyBankAccounts(CancellationToken cancellationToken = default)
    {
        var result = await _bankAccountService.GetMySavedBankAccountsAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<SavedBankAccountDto>> CreateSavedBankAccount(
        [FromBody] CreateSavedBankAccountDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _bankAccountService.CreateSavedBankAccountAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}/delete")]
    public async Task<ActionResult> DeleteSavedBankAccount(int id, CancellationToken cancellationToken = default)
    {
        var result = await _bankAccountService.DeleteSavedBankAccountAsync(id, cancellationToken);
        return result.ToNoContentResult();
    }

    [HttpPut("{id:int}/default")]
    public async Task<ActionResult> SetDefaultBankAccount(int id, CancellationToken cancellationToken = default)
    {
        var result = await _bankAccountService.SetDefaultBankAccountAsync(id, cancellationToken);
        return result.ToNoContentResult();
    }

    [HttpGet("banks")]
    public async Task<ActionResult<List<BankLookupItemDto>>> GetBanks(CancellationToken cancellationToken = default)
    {
        var result = await _bankLookupService.GetBanksAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<string>> LookupOwnerName(
        [FromQuery] string bankCode,
        [FromQuery] string accountNumber,
        CancellationToken cancellationToken = default)
    {
        var result = await _bankLookupService.LookupOwnerNameAsync(bankCode, accountNumber, cancellationToken);
        return result.ToActionResult();
    }
}
