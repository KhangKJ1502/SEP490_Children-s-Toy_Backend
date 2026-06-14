using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Common;
using ToyStore.Application.Common.Models;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Wallets;
using ToyStore.Application.DTOs.Withdrawals;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Enums;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

public class WithdrawalService : IWithdrawalService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IWalletService _walletService;
    private readonly IWithdrawalLedgerService _ledger;
    private readonly IPayOsPayoutService _payos;
    private readonly IWithdrawalPayOsSyncService _syncService;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly WithdrawalLimitsOptions _limits;
    private readonly IMapper _mapper;
    private readonly ILogger<WithdrawalService> _logger;

    public WithdrawalService(
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IWalletService walletService,
        IWithdrawalLedgerService ledger,
        IPayOsPayoutService payos,
        IWithdrawalPayOsSyncService syncService,
        IDomainEventPublisher eventPublisher,
        IOptions<WithdrawalLimitsOptions> limits,
        IMapper mapper,
        ILogger<WithdrawalService> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _walletService = walletService;
        _ledger = ledger;
        _payos = payos;
        _syncService = syncService;
        _eventPublisher = eventPublisher;
        _limits = limits.Value;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<WithdrawalDto>> CreateWithdrawalAsync(
        CreateWithdrawalRequestDto dto,
        CancellationToken ct = default)
    {
        var accountId = _currentUser.AccountId;
        if (accountId <= 0)
            return Result<WithdrawalDto>.Unauthorized();

        // ── 1. Pre-checks: wallet must be Active and have a PIN ──────────────
        var walletResult = await _walletService.GetMyWalletAsync(ct);
        if (walletResult.IsFailure)
            return Result<WithdrawalDto>.Failure(walletResult.ErrorCode!, walletResult.ErrorMessage!);

        var wallet = walletResult.Data!;
        if (wallet.Status != "Active")
            return Result<WithdrawalDto>.BusinessError(WithdrawalErrorMessages.For(WithdrawalErrorCode.WalletNotActive));
        if (!wallet.HasPin)
            return Result<WithdrawalDto>.BusinessError(WithdrawalErrorMessages.For(WithdrawalErrorCode.WalletHasNoPin));
        if (wallet.Status == "Frozen")
            return Result<WithdrawalDto>.BusinessError(WithdrawalErrorMessages.For(WithdrawalErrorCode.WalletFrozen));

        // ── 2. One-at-a-time rule: block new request if one is already in-flight ──
        var hasActive = await _uow.Withdrawals.HasActivePendingAsync(accountId, ct);
        if (hasActive)
            return Result<WithdrawalDto>.BusinessError(WithdrawalErrorMessages.For(WithdrawalErrorCode.WithdrawalInProgress));

        // ── 3. Amount validation ─────────────────────────────────────────────
        if (dto.Amount < _limits.MinAmount)
            return Result<WithdrawalDto>.BusinessError(WithdrawalErrorMessages.For(WithdrawalErrorCode.AmountBelowMinimum));
        if (dto.Amount > _limits.MaxAmountPerTransaction)
            return Result<WithdrawalDto>.BusinessError(WithdrawalErrorMessages.For(WithdrawalErrorCode.AmountAboveMaximum));
        if (dto.Amount > wallet.AvailableBalance)
            return Result<WithdrawalDto>.BusinessError(WithdrawalErrorMessages.For(WithdrawalErrorCode.InsufficientAvailable));

        // ── 3. Daily limits ──────────────────────────────────────────────────
        var (dailyAmount, dailyCount) = await _uow.Withdrawals.GetDailyStatsAsync(accountId, DateTime.UtcNow, ct);
        if (dailyCount >= _limits.MaxCountPerDay)
            return Result<WithdrawalDto>.BusinessError(WithdrawalErrorMessages.For(WithdrawalErrorCode.DailyCountExceeded));
        if (dailyAmount + dto.Amount > _limits.MaxAmountPerDay)
            return Result<WithdrawalDto>.BusinessError(WithdrawalErrorMessages.For(WithdrawalErrorCode.DailyAmountExceeded));

        // ── 4. Resolve bank account ──────────────────────────────────────────
        string bankBin, bankName, accountNumber, accountName;

        if (dto.SavedBankAccountId.HasValue)
        {
            var bank = await _uow.SavedBankAccounts.GetByIdAsync(dto.SavedBankAccountId.Value, ct);
            if (bank is null || bank.IsDeleted || bank.AccountId != accountId)
                return Result<WithdrawalDto>.BusinessError(WithdrawalErrorMessages.For(WithdrawalErrorCode.BankAccountNotFound));

            bankBin = bank.BankBin;
            bankName = bank.BankName;
            accountNumber = bank.AccountNumber;
            accountName = bank.AccountName;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.ToBankBin) || string.IsNullOrWhiteSpace(dto.ToAccountNumber) || string.IsNullOrWhiteSpace(dto.ToAccountName))
                return Result<WithdrawalDto>.BusinessError("Please select a bank account or provide complete bank details.");

            bankBin = dto.ToBankBin!;
            bankName = dto.ToBankName ?? dto.ToBankBin!;
            accountNumber = dto.ToAccountNumber!;
            accountName = dto.ToAccountName!;
        }

        // ── 5. Verify wallet PIN ─────────────────────────────────────────────
        var pinResult = await _walletService.VerifyWalletPinAsync(
            new VerifyWalletPinRequestDto { Pin = dto.Pin, ActionType = WalletPinActions.Withdrawal },
            ct);
        if (pinResult.IsFailure)
            return Result<WithdrawalDto>.Failure(pinResult.ErrorCode!, pinResult.ErrorMessage!);

        // ── 6. Lock funds ────────────────────────────────────────────────────
        var (lockCode, withdrawalRequest) = await _ledger.LockAsync(
            new LockWithdrawalCommand(accountId, dto.Amount, bankBin, bankName, accountNumber, accountName),
            ct);

        if (lockCode != WithdrawalErrorCode.Success || withdrawalRequest is null)
            return Result<WithdrawalDto>.BusinessError(WithdrawalErrorMessages.For(lockCode));

        // ── 7. Call PayOS ────────────────────────────────────────────────────
        var description = $"Rut vi {withdrawalRequest.ReferenceId}";
        var payosResult = await _payos.CreatePayoutAsync(
            withdrawalRequest.ReferenceId,
            dto.Amount,
            bankBin,
            accountNumber,
            accountName,
            description,
            ct);

        if (!payosResult.Success)
        {
            // Rollback locked funds immediately on PayOS failure.
            // Use CancellationToken.None: the request ct may already be cancelled at this point
            // (e.g. client disconnect or response already sent), but we MUST persist the rollback.
            await _ledger.RollbackAsync(
                new RollbackWithdrawalCommand(withdrawalRequest.WithdrawalId, payosResult.ErrorMessage ?? "PayOS payout failed", WithdrawalHistorySources.System),
                CancellationToken.None);

            return Result<WithdrawalDto>.BadGateway("Unable to connect to the payment gateway. Your balance has been restored. Please try again.");
        }

        // ── 8. Update to PROCESSING + store PayOS IDs ────────────────────────
        // Use CancellationToken.None: PayOS accepted the payout, so this DB write MUST succeed
        // even if the originating HTTP request was already cancelled/disconnected.
        withdrawalRequest.Status = WithdrawalStatuses.Processing;
        withdrawalRequest.PayosPayoutId = payosResult.PayoutId;
        withdrawalRequest.PayosTransactionId = payosResult.TransactionId;
        withdrawalRequest.PayosRawResponse = payosResult.RawResponse;
        withdrawalRequest.ProcessingAt = DateTime.UtcNow;
        withdrawalRequest.RetryCount = 1;
        await _uow.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation("Withdrawal {Id} sent to PayOS — payoutId={PayoutId}", withdrawalRequest.WithdrawalId, payosResult.PayoutId);

        return Result<WithdrawalDto>.Success(MapToDto(withdrawalRequest));
    }

    public async Task<Result<WithdrawalDto>> GetWithdrawalAsync(int withdrawalId, CancellationToken ct = default)
    {
        var accountId = _currentUser.AccountId;
        if (accountId <= 0)
            return Result<WithdrawalDto>.Unauthorized();

        var withdrawal = await _uow.Withdrawals.GetByIdAsync(withdrawalId, ct);
        if (withdrawal is null || withdrawal.AccountId != accountId)
            return Result<WithdrawalDto>.NotFound("Withdrawal", withdrawalId);

        // If the withdrawal is still in PROCESSING status and has a PayOS payout ID,
        // trigger an on-demand sync to ensure the user gets the absolute latest status immediately.
        if (withdrawal.Status == WithdrawalStatuses.Processing && !string.IsNullOrEmpty(withdrawal.PayosPayoutId))
        {
            try
            {
                await _syncService.SyncAsync(withdrawalId, ct);
                // Reload from DB to get the newly committed/rolled back state
                var updated = await _uow.Withdrawals.GetByIdAsync(withdrawalId, ct);
                if (updated is not null)
                {
                    withdrawal = updated;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to perform on-demand status sync for withdrawal {Id}", withdrawalId);
            }
        }

        return Result<WithdrawalDto>.Success(MapToDto(withdrawal));
    }

    public async Task<Result<PaginatedResponse<WithdrawalDto>>> GetMyWithdrawalsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var accountId = _currentUser.AccountId;
        if (accountId <= 0)
            return Result<PaginatedResponse<WithdrawalDto>>.Unauthorized();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var total = await _uow.Withdrawals.CountMyWithdrawalsAsync(accountId, ct);
        var items = await _uow.Withdrawals.GetMyWithdrawalsAsync(accountId, page, pageSize, ct);

        var response = new PaginatedResponse<WithdrawalDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize,
        };
        return Result<PaginatedResponse<WithdrawalDto>>.Success(response);
    }

    public async Task<Result> CancelWithdrawalAsync(int withdrawalId, CancellationToken ct = default)
    {
        var accountId = _currentUser.AccountId;
        if (accountId <= 0)
            return Result.Unauthorized();

        var code = await _ledger.CancelAsync(new CancelWithdrawalCommand(withdrawalId, accountId), ct);
        return code switch
        {
            WithdrawalErrorCode.Success  => Result.Success(),
            WithdrawalErrorCode.InvalidStatus => Result.BusinessError(WithdrawalErrorMessages.For(WithdrawalErrorCode.InvalidStatus)),
            _ => Result.BusinessError(WithdrawalErrorMessages.For(code)),
        };
    }

    private static WithdrawalDto MapToDto(Domain.Entities.WithdrawalRequest w) => new()
    {
        WithdrawalId = w.WithdrawalId,
        ReferenceId = w.ReferenceId,
        Amount = w.Amount,
        ToBankBin = w.ToBankBin,
        ToBankName = w.ToBankName,
        ToAccountNumber = w.ToAccountNumber,
        ToAccountName = w.ToAccountName,
        PayosPayoutId = w.PayosPayoutId,
        Status = w.Status,
        FailReason = w.FailReason,
        ProcessingAt = w.ProcessingAt,
        CompletedAt = w.CompletedAt,
        CancelledAt = w.CancelledAt,
        CreatedAt = w.CreatedAt,
    };
}
