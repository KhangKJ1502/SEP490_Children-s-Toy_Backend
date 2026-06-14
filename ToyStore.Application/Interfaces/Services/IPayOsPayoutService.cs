namespace ToyStore.Application.Interfaces.Services;

public record PayOsPayoutResult(bool Success, string? PayoutId, string? TransactionId, string? RawResponse, string? ErrorMessage);

/// <summary>
/// Result from GET /v1/payouts/{payoutId}.
/// ApprovalState values: PROCESSING, SUCCEEDED, FAILED (and other terminal states).
/// </summary>
public record PayOsPayoutDetailResult(
    bool Success,
    string? ApprovalState,
    string? TransactionState,
    string? TransactionId,
    string? RawResponse,
    string? ErrorMessage);

public interface IPayOsPayoutService
{
    /// <summary>
    /// Sends a payout request to PayOS. Retries up to 3 times with exponential back-off (2s → 4s → 8s).
    /// </summary>
    Task<PayOsPayoutResult> CreatePayoutAsync(
        string referenceId,
        decimal amount,
        string toBankBin,
        string toAccountNumber,
        string toAccountName,
        string description,
        CancellationToken ct = default);

    /// <summary>
    /// Queries the current state of a payout by its PayOS payout ID.
    /// Used by the poll job to determine whether to Commit or Rollback.
    /// </summary>
    Task<PayOsPayoutDetailResult> GetPayoutAsync(string payoutId, CancellationToken ct = default);
}
