namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Credits refund amounts to the customer's internal wallet (creates wallet if missing).
/// </summary>
public interface IWalletRefundCreditor
{
    /// <summary>
    /// Idempotent credit — at most one completed refund wallet txn per order (key REFUND_{orderCode}).
    /// </summary>
    Task<bool> CreditRefundAsync(
        int accountId,
        decimal amount,
        string orderCode,
        int? relatedOrderId = null,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null);
}
