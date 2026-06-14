using Microsoft.Extensions.Options;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Enums;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Last-resort safety net: rolls back withdrawal requests stuck in PENDING or PROCESSING
/// for longer than the configured timeout. Runs every 5 minutes.
/// For PROCESSING withdrawals with a PayosPayoutId, the poll job is attempted first —
/// this job only fires its own rollback when the payout is still PROCESSING after timeout.
/// CommitAsync / RollbackAsync are idempotent, so a concurrent commit from the poll job
/// is handled safely via the ledger's idempotency guard.
/// </summary>
public class WithdrawalTimeoutJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WithdrawalTimeoutJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public WithdrawalTimeoutJob(IServiceProvider services, ILogger<WithdrawalTimeoutJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "WithdrawalTimeoutJob error"); }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var ledger = scope.ServiceProvider.GetRequiredService<IWithdrawalLedgerService>();
        var syncService = scope.ServiceProvider.GetRequiredService<IWithdrawalPayOsSyncService>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
        var limits = scope.ServiceProvider.GetRequiredService<IOptions<WithdrawalLimitsOptions>>().Value;

        var cutoff = DateTime.UtcNow.AddMinutes(-limits.TimeoutMinutes);
        var stale = await uow.Withdrawals.GetStalePendingAsync(cutoff, ct);

        _logger.LogInformation("WithdrawalTimeoutJob: found {Count} stale withdrawals", stale.Count);

        foreach (var withdrawal in stale)
        {
            try
            {
                // For PROCESSING withdrawals that have been sent to PayOS, give the poll job one
                // final chance to resolve the correct terminal state before forcefully timing out.
                if (withdrawal.Status == WithdrawalStatuses.Processing
                    && !string.IsNullOrEmpty(withdrawal.PayosPayoutId))
                {
                    var synced = await syncService.SyncAsync(withdrawal.WithdrawalId, ct);
                    if (synced)
                    {
                        _logger.LogInformation("WithdrawalTimeoutJob: poll-synced withdrawal {Id} — skipping forced rollback", withdrawal.WithdrawalId);
                        continue;
                    }
                    // SyncAsync returned false (PayOS still PROCESSING) → fall through to timeout rollback
                }

                var code = await ledger.RollbackAsync(
                    new RollbackWithdrawalCommand(
                        withdrawal.WithdrawalId,
                        $"Timeout after {limits.TimeoutMinutes} minutes",
                        WithdrawalHistorySources.Job),
                    ct);

                if (code == Application.Common.WithdrawalErrorCode.Success)
                {
                    _logger.LogInformation("WithdrawalTimeoutJob: rolled back withdrawal {Id}", withdrawal.WithdrawalId);

                    try
                    {
                        await eventPublisher.PublishAsync(
                            "Wallet",
                            withdrawal.WithdrawalId.ToString(),
                            NotificationEventTypes.WalletWithdrawalFailed,
                            new
                            {
                                accountId = withdrawal.AccountId,
                                withdrawalId = withdrawal.WithdrawalId,
                                amount = withdrawal.Amount,
                                bankName = withdrawal.ToBankName,
                                accountNumber = withdrawal.ToAccountNumber,
                                failReason = $"Timeout after {limits.TimeoutMinutes} minutes",
                            },
                            ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "WithdrawalTimeoutJob: notification publish failed for withdrawal {Id}", withdrawal.WithdrawalId);
                    }
                }
                else
                {
                    _logger.LogWarning("WithdrawalTimeoutJob: rollback returned {Code} for withdrawal {Id}", code, withdrawal.WithdrawalId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WithdrawalTimeoutJob: error processing withdrawal {Id}", withdrawal.WithdrawalId);
            }
        }
    }
}
