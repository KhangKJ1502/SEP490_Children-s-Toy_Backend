using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Enums;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Queries PayOS for the latest state of a PROCESSING withdrawal and
/// calls CommitAsync or RollbackAsync, then publishes a push notification.
/// Shared by WithdrawalPayoutPollJob and any ad-hoc sync.
/// </summary>
public class WithdrawalPayOsSyncService : IWithdrawalPayOsSyncService
{
    private readonly SEP490ToyStoreContext _db;
    private readonly IPayOsPayoutService _payos;
    private readonly IWithdrawalLedgerService _ledger;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<WithdrawalPayOsSyncService> _logger;

    public WithdrawalPayOsSyncService(
        SEP490ToyStoreContext db,
        IPayOsPayoutService payos,
        IWithdrawalLedgerService ledger,
        IDomainEventPublisher eventPublisher,
        ILogger<WithdrawalPayOsSyncService> logger)
    {
        _db = db;
        _payos = payos;
        _ledger = ledger;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<bool> SyncAsync(int withdrawalId, CancellationToken ct = default)
    {
        var withdrawal = await _db.WithdrawalRequests
            .FirstOrDefaultAsync(w => w.WithdrawalId == withdrawalId, ct);

        if (withdrawal is null)
        {
            _logger.LogWarning("WithdrawalPayOsSyncService: withdrawal {Id} not found", withdrawalId);
            return false;
        }

        if (withdrawal.Status != WithdrawalStatuses.Processing)
        {
            _logger.LogDebug("WithdrawalPayOsSyncService: withdrawal {Id} is already in terminal status {Status} — skipping", withdrawalId, withdrawal.Status);
            return withdrawal.Status != WithdrawalStatuses.Pending; // treat non-Processing as "done"
        }

        if (string.IsNullOrEmpty(withdrawal.PayosPayoutId))
        {
            _logger.LogWarning("WithdrawalPayOsSyncService: withdrawal {Id} has no PayosPayoutId — cannot poll", withdrawalId);
            return false;
        }

        var detail = await _payos.GetPayoutAsync(withdrawal.PayosPayoutId, ct);

        if (!detail.Success)
        {
            _logger.LogWarning("WithdrawalPayOsSyncService: GetPayout failed for withdrawal {Id} payoutId={PayoutId}: {Err}",
                withdrawalId, withdrawal.PayosPayoutId, detail.ErrorMessage);
            return false;
        }

        var approvalState = detail.ApprovalState?.ToUpperInvariant();
        _logger.LogInformation("WithdrawalPayOsSyncService: withdrawal {Id} approvalState={State}", withdrawalId, approvalState);

        switch (approvalState)
        {
            case "SUCCEEDED":
            case "COMPLETED":
            case "SUCCESS":
                var commitCode = await _ledger.CommitAsync(
                    new CommitWithdrawalCommand(withdrawalId, detail.TransactionId, detail.RawResponse),
                    ct);

                if (commitCode == WithdrawalErrorCode.Success)
                {
                    await PublishNotificationAsync(
                        withdrawal.AccountId, withdrawalId, withdrawal.Amount,
                        withdrawal.ToBankName, withdrawal.ToAccountNumber,
                        failReason: null, isSuccess: true, ct);
                    _logger.LogInformation("WithdrawalPayOsSyncService: committed withdrawal {Id}", withdrawalId);
                }
                else
                {
                    _logger.LogError("WithdrawalPayOsSyncService: CommitAsync returned {Code} for withdrawal {Id}", commitCode, withdrawalId);
                }
                return true;

            case "FAILED":
            case "CANCELLED":
            case "REJECTED":
                var failReason = $"PayOS payout {approvalState?.ToLower()}: {detail.TransactionState ?? "no details"}";
                var rollbackCode = await _ledger.RollbackAsync(
                    new RollbackWithdrawalCommand(withdrawalId, failReason, WithdrawalHistorySources.Job),
                    ct);

                if (rollbackCode == WithdrawalErrorCode.Success)
                {
                    await PublishNotificationAsync(
                        withdrawal.AccountId, withdrawalId, withdrawal.Amount,
                        withdrawal.ToBankName, withdrawal.ToAccountNumber,
                        failReason, isSuccess: false, ct);
                    _logger.LogInformation("WithdrawalPayOsSyncService: rolled back withdrawal {Id} reason={Reason}", withdrawalId, failReason);
                }
                else
                {
                    _logger.LogError("WithdrawalPayOsSyncService: RollbackAsync returned {Code} for withdrawal {Id}", rollbackCode, withdrawalId);
                }
                return true;

            default:
                // PROCESSING or unknown — poll again later
                return false;
        }
    }

    private async Task PublishNotificationAsync(
        int accountId,
        int withdrawalId,
        decimal amount,
        string bankName,
        string accountNumber,
        string? failReason,
        bool isSuccess,
        CancellationToken ct)
    {
        try
        {
            var eventType = isSuccess
                ? NotificationEventTypes.WalletWithdrawalSuccess
                : NotificationEventTypes.WalletWithdrawalFailed;

            await _eventPublisher.PublishAsync(
                "Wallet",
                withdrawalId.ToString(),
                eventType,
                new { accountId, amount, withdrawalId, bankName, accountNumber, failReason },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WithdrawalPayOsSyncService: notification publish failed for withdrawal {Id}", withdrawalId);
        }
    }
}
