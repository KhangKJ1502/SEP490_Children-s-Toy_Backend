using Microsoft.Extensions.Logging;
using ToyStore.Application.Common;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Infrastructure.Services;

public class WithdrawalLedgerService : IWithdrawalLedgerService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WithdrawalLedgerService> _logger;

    public WithdrawalLedgerService(
        IUnitOfWork uow,
        ILogger<WithdrawalLedgerService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // LOCK — creates PENDING withdrawal and reserves LockedBalance
    // ────────────────────────────────────────────────────────────────────────────

    public async Task<(WithdrawalErrorCode Code, WithdrawalRequest? Request)> LockAsync(
        LockWithdrawalCommand command,
        CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(ct);
        try
        {
            // 1. Read wallet with UPDLOCK to prevent concurrent race conditions
            var wallet = await _uow.Wallets.GetForUpdateAsync(command.AccountId, ct);
            if (wallet is null)
            {
                await _uow.RollbackTransactionAsync(ct);
                return (WithdrawalErrorCode.WalletNotFound, null);
            }

            if (wallet.Status == "Frozen")
            {
                await _uow.RollbackTransactionAsync(ct);
                return (WithdrawalErrorCode.WalletFrozen, null);
            }

            var available = wallet.Balance - wallet.LockedBalance;
            if (available < command.Amount)
            {
                await _uow.RollbackTransactionAsync(ct);
                return (WithdrawalErrorCode.InsufficientAvailable, null);
            }

            // 2. Reserve funds atomically
            var referenceId = $"WD{command.AccountId}{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            var affected = await _uow.Wallets.IncrementLockedBalanceAsync(command.AccountId, command.Amount, ct);
            if (affected == 0)
            {
                await _uow.RollbackTransactionAsync(ct);
                return (WithdrawalErrorCode.InsufficientAvailable, null);
            }

            // 3. Create PENDING withdrawal request
            var withdrawal = new WithdrawalRequest
            {
                WalletId = wallet.WalletId,
                AccountId = command.AccountId,
                ReferenceId = referenceId,
                Amount = command.Amount,
                ToBankBin = command.ToBankBin,
                ToBankName = command.ToBankName,
                ToAccountNumber = command.ToAccountNumber,
                ToAccountName = command.ToAccountName,
                Status = WithdrawalStatuses.Pending,
                RetryCount = 0,
                CreatedAt = DateTime.UtcNow,
            };
            await _uow.Withdrawals.AddAsync(withdrawal, ct);
            await _uow.SaveChangesAsync(ct);

            // 4. Audit history
            await _uow.Withdrawals.AddStatusHistoryAsync(
                BuildHistory(withdrawal.WithdrawalId, null, WithdrawalStatuses.Pending, WithdrawalHistorySources.User, "Withdrawal requested"),
                ct);
            await _uow.SaveChangesAsync(ct);

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation("Withdrawal {RefId} locked {Amount} for account {AccountId}", referenceId, command.Amount, command.AccountId);
            return (WithdrawalErrorCode.Success, withdrawal);
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "LockAsync failed for account {AccountId}", command.AccountId);
            return (WithdrawalErrorCode.SystemError, null);
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // COMMIT — deducts Balance after successful PayOS payout
    // ────────────────────────────────────────────────────────────────────────────

    public async Task<WithdrawalErrorCode> CommitAsync(
        CommitWithdrawalCommand command,
        CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var withdrawal = await _uow.Withdrawals.GetForUpdateAsync(command.WithdrawalId, ct);
            if (withdrawal is null)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.WalletNotFound;
            }

            // Idempotency: already committed
            if (withdrawal.Status == WithdrawalStatuses.Success)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.Success;
            }

            if (withdrawal.Status != WithdrawalStatuses.Pending && withdrawal.Status != WithdrawalStatuses.Processing)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.InvalidStatus;
            }

            var wallet = await _uow.Wallets.GetForUpdateAsync(withdrawal.AccountId, ct);
            if (wallet is null)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.WalletNotFound;
            }

            var balanceBefore = wallet.Balance;
            var balanceAfter  = balanceBefore - withdrawal.Amount;

            // Deduct Balance and LockedBalance atomically
            var affected = await _uow.Wallets.CommitDeductionAsync(wallet.WalletId, withdrawal.Amount, ct);
            if (affected == 0)
            {
                await _uow.RollbackTransactionAsync(ct);
                _logger.LogError("Commit failed — insufficient balance for withdrawal {Id}", command.WithdrawalId);
                return WithdrawalErrorCode.InsufficientAvailable;
            }

            // Insert WalletTransaction
            var txn = new WalletTransaction
            {
                WalletId = wallet.WalletId,
                AccountId = withdrawal.AccountId,
                TxnType = WalletTxnTypes.Withdrawal,
                Direction = "DR",
                Amount = withdrawal.Amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                Method = "BankTransfer",
                ExternalRef = withdrawal.PayosPayoutId,
                IdempotencyKey = withdrawal.ReferenceId,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
            };
            await _uow.Wallets.AddTransactionAsync(txn, ct);
            await _uow.SaveChangesAsync(ct);

            // Update withdrawal
            var prevStatus = withdrawal.Status;
            withdrawal.WalletTransactionId = txn.WalletTransactionId;
            withdrawal.Status = WithdrawalStatuses.Success;
            withdrawal.PayosTransactionId = command.PayosTransactionId;
            withdrawal.PayosRawResponse = command.PayosRawResponse;
            withdrawal.CompletedAt = DateTime.UtcNow;
            _uow.Withdrawals.UpdateAsync(withdrawal);
            await _uow.SaveChangesAsync(ct);

            await _uow.Withdrawals.AddStatusHistoryAsync(
                BuildHistory(withdrawal.WithdrawalId, prevStatus, WithdrawalStatuses.Success, WithdrawalHistorySources.Job, null),
                ct);
            await _uow.SaveChangesAsync(ct);

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation("Withdrawal {Id} committed — balance deducted {Amount}", command.WithdrawalId, withdrawal.Amount);
            return WithdrawalErrorCode.Success;
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "CommitAsync failed for withdrawal {Id}", command.WithdrawalId);
            return WithdrawalErrorCode.SystemError;
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // ROLLBACK — releases LockedBalance on failure / timeout
    // ────────────────────────────────────────────────────────────────────────────

    public async Task<WithdrawalErrorCode> RollbackAsync(
        RollbackWithdrawalCommand command,
        CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var withdrawal = await _uow.Withdrawals.GetForUpdateAsync(command.WithdrawalId, ct);
            if (withdrawal is null)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.WalletNotFound;
            }

            // Idempotency: already in terminal state
            if (withdrawal.Status is WithdrawalStatuses.Failed or WithdrawalStatuses.Cancelled or WithdrawalStatuses.Success)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.Success;
            }

            if (withdrawal.Status != WithdrawalStatuses.Pending && withdrawal.Status != WithdrawalStatuses.Processing)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.InvalidStatus;
            }

            // Release LockedBalance only (Balance unchanged)
            await _uow.Wallets.DecrementLockedBalanceAsync(withdrawal.AccountId, withdrawal.Amount, ct);

            var prevStatus = withdrawal.Status;
            withdrawal.Status = WithdrawalStatuses.Failed;
            withdrawal.FailReason = command.FailReason;
            withdrawal.CompletedAt = DateTime.UtcNow;
            _uow.Withdrawals.UpdateAsync(withdrawal);
            await _uow.SaveChangesAsync(ct);

            await _uow.Withdrawals.AddStatusHistoryAsync(
                BuildHistory(withdrawal.WithdrawalId, prevStatus, WithdrawalStatuses.Failed, command.Source, command.FailReason),
                ct);
            await _uow.SaveChangesAsync(ct);

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation("Withdrawal {Id} rolled back — {Reason}", command.WithdrawalId, command.FailReason);
            return WithdrawalErrorCode.Success;
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "RollbackAsync failed for withdrawal {Id}", command.WithdrawalId);
            return WithdrawalErrorCode.SystemError;
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // CANCEL — cancels a PENDING withdrawal that was never sent to PayOS
    // ────────────────────────────────────────────────────────────────────────────

    public async Task<WithdrawalErrorCode> CancelAsync(
        CancelWithdrawalCommand command,
        CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var withdrawal = await _uow.Withdrawals.GetForUpdateAsync(command.WithdrawalId, ct);
            if (withdrawal is null)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.WalletNotFound;
            }

            if (withdrawal.AccountId != command.RequestingAccountId)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.InvalidStatus;
            }

            if (withdrawal.Status != WithdrawalStatuses.Pending || withdrawal.PayosPayoutId is not null)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.InvalidStatus;
            }

            // Release LockedBalance
            await _uow.Wallets.DecrementLockedBalanceAsync(withdrawal.AccountId, withdrawal.Amount, ct);

            var prevStatus = withdrawal.Status;
            withdrawal.Status = WithdrawalStatuses.Cancelled;
            withdrawal.CancelledAt = DateTime.UtcNow;
            _uow.Withdrawals.UpdateAsync(withdrawal);
            await _uow.SaveChangesAsync(ct);

            await _uow.Withdrawals.AddStatusHistoryAsync(
                BuildHistory(withdrawal.WithdrawalId, prevStatus, WithdrawalStatuses.Cancelled, WithdrawalHistorySources.User, "Cancelled by user"),
                ct);
            await _uow.SaveChangesAsync(ct);

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation("Withdrawal {Id} cancelled by account {AccountId}", command.WithdrawalId, command.RequestingAccountId);
            return WithdrawalErrorCode.Success;
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "CancelAsync failed for withdrawal {Id}", command.WithdrawalId);
            return WithdrawalErrorCode.SystemError;
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────────

    private static WithdrawalStatusHistory BuildHistory(
        int withdrawalId,
        string? fromStatus,
        string toStatus,
        string source,
        string? note)
        => new()
        {
            WithdrawalId = withdrawalId,
            FromStatus   = fromStatus,
            ToStatus     = toStatus,
            Source       = source,
            Note         = note,
            CreatedAt    = DateTime.UtcNow,
        };
}
