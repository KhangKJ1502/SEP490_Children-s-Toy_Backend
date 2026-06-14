using ToyStore.Application.Common;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public record LockWithdrawalCommand(
    int AccountId,
    decimal Amount,
    string ToBankBin,
    string ToBankName,
    string ToAccountNumber,
    string ToAccountName);

public record CommitWithdrawalCommand(
    int WithdrawalId,
    string? PayosTransactionId,
    string? PayosRawResponse);

public record RollbackWithdrawalCommand(
    int WithdrawalId,
    string FailReason,
    string Source);

public record CancelWithdrawalCommand(
    int WithdrawalId,
    int RequestingAccountId);

public interface IWithdrawalLedgerService
{
    /// <summary>
    /// Locks the requested amount in the wallet and creates a PENDING withdrawal request.
    /// Returns the created WithdrawalRequest on success.
    /// </summary>
    Task<(WithdrawalErrorCode Code, WithdrawalRequest? Request)> LockAsync(
        LockWithdrawalCommand command,
        CancellationToken ct = default);

    /// <summary>
    /// Commits a successful payout: deducts Balance + LockedBalance and records the wallet transaction.
    /// Idempotent — returns success if already in SUCCESS state.
    /// </summary>
    Task<WithdrawalErrorCode> CommitAsync(
        CommitWithdrawalCommand command,
        CancellationToken ct = default);

    /// <summary>
    /// Rolls back a failed/timed-out payout: releases LockedBalance without touching Balance.
    /// Idempotent — skips if already in a terminal state (FAILED, CANCELLED, SUCCESS).
    /// </summary>
    Task<WithdrawalErrorCode> RollbackAsync(
        RollbackWithdrawalCommand command,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels a PENDING withdrawal that has NOT yet been sent to PayOS (PayosPayoutId IS NULL).
    /// </summary>
    Task<WithdrawalErrorCode> CancelAsync(
        CancelWithdrawalCommand command,
        CancellationToken ct = default);
}
