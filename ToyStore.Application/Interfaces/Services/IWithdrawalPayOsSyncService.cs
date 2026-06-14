namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Queries PayOS for the current state of a single PROCESSING withdrawal and
/// calls CommitAsync or RollbackAsync accordingly.
/// Used by both WithdrawalPayoutPollJob and (optionally) ad-hoc sync endpoints.
/// </summary>
public interface IWithdrawalPayOsSyncService
{
    /// <summary>
    /// Syncs a single withdrawal with PayOS.
    /// Returns true if a terminal action (Commit or Rollback) was taken; false if still PROCESSING.
    /// </summary>
    Task<bool> SyncAsync(int withdrawalId, CancellationToken ct = default);
}
