using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Processes PayOS payout webhook callbacks.
/// Logs every incoming webhook, verifies signature, and calls Commit/Rollback on the ledger.
/// Always returns HTTP 200 to PayOS (caller must swallow exceptions after logging).
/// </summary>
public class PayOsPayoutWebhookService : IPayOsPayoutWebhookService
{
    private readonly SEP490ToyStoreContext _db;
    private readonly IWithdrawalLedgerService _ledger;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly PayOsOptions _opts;
    private readonly ILogger<PayOsPayoutWebhookService> _logger;

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public PayOsPayoutWebhookService(
        SEP490ToyStoreContext db,
        IWithdrawalLedgerService ledger,
        IDomainEventPublisher eventPublisher,
        IOptions<PayOsOptions> opts,
        ILogger<PayOsPayoutWebhookService> logger)
    {
        _db = db;
        _ledger = ledger;
        _eventPublisher = eventPublisher;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task HandleAsync(string rawPayload, CancellationToken ct = default)
    {
        // 1. Parse payload
        PayOsPayoutWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PayOsPayoutWebhookPayload>(rawPayload, _json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse PayOS payout webhook: {Payload}", rawPayload);
            return;
        }

        var referenceId = payload?.Data?.Reference;
        var eventType = payload?.Code == "00" ? "payout.success" : "payout.failed";

        // 2. Log the webhook before any processing
        var log = new PayosWebhookLog
        {
            ReferenceId = referenceId,
            WebhookEventId = referenceId,
            EventType = eventType,
            RawPayload = rawPayload,
            ProcessStatus = "RECEIVED",
            IsSignatureValid = false,
            CreatedAt = DateTime.UtcNow,
        };
        await _db.PayosWebhookLogs.AddAsync(log, ct);
        await _db.SaveChangesAsync(ct);

        // 3. Verify signature (sorted query string HMAC)
        var isValid = VerifyWebhookSignature(payload);
        log.IsSignatureValid = isValid;

        if (!isValid)
        {
            log.ProcessStatus = "IGNORED";
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("PayOS payout webhook signature invalid — referenceId={Ref}", referenceId);
            return;
        }

        // 4. Duplicate detection — same referenceId already PROCESSED
        var alreadyProcessed = await _db.PayosWebhookLogs
            .AnyAsync(l => l.ReferenceId == referenceId && l.ProcessStatus == "PROCESSED" && l.WebhookLogId != log.WebhookLogId, ct);

        if (alreadyProcessed)
        {
            log.ProcessStatus = "IGNORED";
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("PayOS payout webhook duplicate — referenceId={Ref}", referenceId);
            return;
        }

        // 5. Lookup withdrawal
        var withdrawal = await _db.WithdrawalRequests
            .FirstOrDefaultAsync(w => w.ReferenceId == referenceId, ct);

        if (withdrawal is null)
        {
            log.ProcessStatus = "IGNORED";
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("PayOS payout webhook: no withdrawal found for referenceId={Ref}", referenceId);
            return;
        }

        log.WithdrawalId = withdrawal.WithdrawalId;

        // 6. Verify amount matches
        if (payload?.Data is not null && payload.Data.Amount != withdrawal.Amount)
        {
            log.ProcessStatus = "ERROR";
            await _db.SaveChangesAsync(ct);
            _logger.LogError("PayOS payout webhook amount mismatch — expected {Expected} got {Got} for {Ref}", withdrawal.Amount, payload.Data.Amount, referenceId);
            return;
        }

        // 7. Commit or Rollback based on success flag
        bool isSuccess = payload?.Code == "00" && payload.Success;

        try
        {
            if (isSuccess)
            {
                var code = await _ledger.CommitAsync(
                    new CommitWithdrawalCommand(withdrawal.WithdrawalId, payload?.Data?.TransactionId, rawPayload),
                    ct);

                if (code == Application.Common.WithdrawalErrorCode.Success)
                {
                    log.ProcessStatus = "PROCESSED";
                    await _db.SaveChangesAsync(ct);

                    await PublishNotificationAsync(withdrawal.AccountId, withdrawal.WithdrawalId, withdrawal.Amount,
                        withdrawal.ToBankName, withdrawal.ToAccountNumber, null, isSuccess: true, ct);
                }
                else
                {
                    log.ProcessStatus = "ERROR";
                    await _db.SaveChangesAsync(ct);
                }
            }
            else
            {
                var failReason = payload?.Desc ?? "PayOS payout failed";
                var code = await _ledger.RollbackAsync(
                    new RollbackWithdrawalCommand(withdrawal.WithdrawalId, failReason, WithdrawalHistorySources.Webhook),
                    ct);

                if (code == Application.Common.WithdrawalErrorCode.Success)
                {
                    log.ProcessStatus = "PROCESSED";
                    await _db.SaveChangesAsync(ct);

                    await PublishNotificationAsync(withdrawal.AccountId, withdrawal.WithdrawalId, withdrawal.Amount,
                        withdrawal.ToBankName, withdrawal.ToAccountNumber, failReason, isSuccess: false, ct);
                }
                else
                {
                    log.ProcessStatus = "ERROR";
                    await _db.SaveChangesAsync(ct);
                }
            }
        }
        catch (Exception ex)
        {
            log.ProcessStatus = "ERROR";
            await _db.SaveChangesAsync(ct);
            _logger.LogError(ex, "PayOS payout webhook processing failed for referenceId={Ref}", referenceId);
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
            // Notification failure must never roll back the withdrawal
            _logger.LogWarning(ex, "Failed to publish withdrawal notification for withdrawal {Id}", withdrawalId);
        }
    }

    private bool VerifyWebhookSignature(PayOsPayoutWebhookPayload? payload)
    {
        if (payload?.Data is null || string.IsNullOrWhiteSpace(payload.Signature))
            return false;

        // PayOS webhook signature: HMAC-SHA256 of sorted query-string-style data fields
        var dataFields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["amount"] = ((long)payload.Data.Amount).ToString(),
            ["description"] = payload.Data.Description ?? string.Empty,
            ["id"] = payload.Data.Id ?? string.Empty,
            ["reference"] = payload.Data.Reference ?? string.Empty,
            ["status"] = payload.Data.Status ?? string.Empty,
        };

        var dataToSign = string.Join("&", dataFields.Select(kv => $"{kv.Key}={kv.Value}"));
        var key = Encoding.UTF8.GetBytes(_opts.ChecksumKey);
        var data = Encoding.UTF8.GetBytes(dataToSign);
        using var hmac = new HMACSHA256(key);
        var computed = Convert.ToHexString(hmac.ComputeHash(data)).ToLowerInvariant();
        return string.Equals(computed, payload.Signature, StringComparison.OrdinalIgnoreCase);
    }
}
