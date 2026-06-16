using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services;

public class WithdrawalLedgerService : IWithdrawalLedgerService
{
    private readonly SEP490ToyStoreContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WithdrawalLedgerService> _logger;

    public WithdrawalLedgerService(
        SEP490ToyStoreContext db,
        IUnitOfWork uow,
        ILogger<WithdrawalLedgerService> logger)
    {
        _db = db;
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
            var wallet = await GetWalletForUpdateAsync(command.AccountId, ct);
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

            // 2. Reserve funds
            var referenceId = $"WD{command.AccountId}{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Wallets SET LockedBalance = LockedBalance + {command.Amount} WHERE AccountID = {command.AccountId} AND (Balance - LockedBalance) >= {command.Amount}",
                ct);

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
            await _db.WithdrawalRequests.AddAsync(withdrawal, ct);
            await _db.SaveChangesAsync(ct);

            // 4. Audit history
            await AppendStatusHistoryAsync(withdrawal.WithdrawalId, null, WithdrawalStatuses.Pending, WithdrawalHistorySources.User, "Withdrawal requested", ct);

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
            var withdrawal = await GetWithdrawalForUpdateAsync(command.WithdrawalId, ct);
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

            var wallet = await GetWalletForUpdateAsync(withdrawal.AccountId, ct);
            if (wallet is null)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.WalletNotFound;
            }

            var balanceBefore = wallet.Balance;
            var balanceAfter  = balanceBefore - withdrawal.Amount;

            // Deduct Balance and LockedBalance atomically
            var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Wallets SET Balance = Balance - {withdrawal.Amount}, LockedBalance = LockedBalance - {withdrawal.Amount} WHERE WalletID = {wallet.WalletId} AND Balance >= {withdrawal.Amount} AND LockedBalance >= {withdrawal.Amount}",
                ct);

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
            await _db.WalletTransactions.AddAsync(txn, ct);
            await _db.SaveChangesAsync(ct);

            // Update withdrawal
            var prevStatus = withdrawal.Status;
            withdrawal.WalletTransactionId = txn.WalletTransactionId;
            withdrawal.Status = WithdrawalStatuses.Success;
            withdrawal.PayosTransactionId = command.PayosTransactionId;
            withdrawal.PayosRawResponse = command.PayosRawResponse;
            withdrawal.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await AppendStatusHistoryAsync(withdrawal.WithdrawalId, prevStatus, WithdrawalStatuses.Success, WithdrawalHistorySources.Job, null, ct);
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
            var withdrawal = await GetWithdrawalForUpdateAsync(command.WithdrawalId, ct);
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
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Wallets SET LockedBalance = LockedBalance - {withdrawal.Amount} WHERE AccountID = {withdrawal.AccountId} AND LockedBalance >= {withdrawal.Amount}",
                ct);

            var prevStatus = withdrawal.Status;
            withdrawal.Status = WithdrawalStatuses.Failed;
            withdrawal.FailReason = command.FailReason;
            withdrawal.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await AppendStatusHistoryAsync(withdrawal.WithdrawalId, prevStatus, WithdrawalStatuses.Failed, command.Source, command.FailReason, ct);
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
            var withdrawal = await GetWithdrawalForUpdateAsync(command.WithdrawalId, ct);
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
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Wallets SET LockedBalance = LockedBalance - {withdrawal.Amount} WHERE AccountID = {withdrawal.AccountId} AND LockedBalance >= {withdrawal.Amount}",
                ct);

            var prevStatus = withdrawal.Status;
            withdrawal.Status = WithdrawalStatuses.Cancelled;
            withdrawal.CancelledAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await AppendStatusHistoryAsync(withdrawal.WithdrawalId, prevStatus, WithdrawalStatuses.Cancelled, WithdrawalHistorySources.User, "Cancelled by user", ct);
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
    // Helpers — UPDLOCK reads
    // ────────────────────────────────────────────────────────────────────────────

    private async Task<Wallet?> GetWalletForUpdateAsync(int accountId, CancellationToken ct)
    {
        // Issue UPDLOCK hint then read through EF
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM Wallets WITH (UPDLOCK, ROWLOCK) WHERE AccountID = {accountId}",
            ct);
        return await _db.Wallets.FirstOrDefaultAsync(w => w.AccountId == accountId, ct);
    }

    private async Task<WithdrawalRequest?> GetWithdrawalForUpdateAsync(int withdrawalId, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM WithdrawalRequests WITH (UPDLOCK, ROWLOCK) WHERE WithdrawalID = {withdrawalId}",
            ct);
        return await _db.WithdrawalRequests.FirstOrDefaultAsync(w => w.WithdrawalId == withdrawalId, ct);
    }

    private async Task AppendStatusHistoryAsync(
        int withdrawalId,
        string? fromStatus,
        string toStatus,
        string source,
        string? note,
        CancellationToken ct)
    {
        var entry = new WithdrawalStatusHistory
        {
            WithdrawalId = withdrawalId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Source = source,
            Note = note,
            CreatedAt = DateTime.UtcNow,
        };
        await _db.WithdrawalStatusHistories.AddAsync(entry, ct);
        await _db.SaveChangesAsync(ct);
    }
}
