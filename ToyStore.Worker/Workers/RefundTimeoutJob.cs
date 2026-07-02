using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Background job that scans for stale unpaid refunds where customer deadline of 48h has expired.
/// Automatically sets CustomerResponse = "Disposed", sets ReturnToCustomerFeePaid = true,
/// and transitions the refund to RefundCompleted (marking items as disposed without generating GHN waybills).
/// </summary>
public class RefundTimeoutJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RefundTimeoutJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Run scan every 5 minutes

    public RefundTimeoutJob(IServiceProvider services, ILogger<RefundTimeoutJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RefundTimeoutJob executed with error.");
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var refundService = scope.ServiceProvider.GetRequiredService<IRefundService>();

        var cutoff = DateTime.UtcNow;
        var staleRefunds = await uow.Refunds.GetStaleUnpaidRefundsAsync(cutoff, ct);

        if (staleRefunds.Count > 0)
        {
            _logger.LogInformation("RefundTimeoutJob: Found {Count} stale unpaid refunds past deadline.", staleRefunds.Count);
        }

        foreach (var refund in staleRefunds)
        {
            try
            {
                _logger.LogInformation("RefundTimeoutJob: Auto-disposing stale unpaid refund {Id} (Code: {Code}) due to 48h timeout.", refund.RefundId, refund.RefundCode);

                // Update database fields directly before invoking complete transition
                refund.CustomerResponse = "Disposed";
                refund.ReturnToCustomerFeePaid = true;
                refund.UpdatedAt = DateTime.UtcNow;
                await uow.SaveChangesAsync(ct);

                var completeDto = new UpdateRefundStatusDto
                {
                    Status = "RefundCompleted",
                    AdminNote = "System Auto-Completed: Return shipping fee shortfall unpaid after 48h. Damaged products marked as Disposed."
                };

                var result = await refundService.UpdateRefundStatusAsync(
                    staffId: refund.ApprovedBy ?? 1, // Fallback to system user ID
                    roleId: 2, // Staff role
                    refundId: refund.RefundId,
                    dto: completeDto,
                    isAdmin: true,
                    cancellationToken: ct);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("RefundTimeoutJob: Successfully auto-completed stale refund {Id}.", refund.RefundId);
                }
                else
                {
                    _logger.LogError("RefundTimeoutJob: Failed to complete stale refund {Id}: {Error}", refund.RefundId, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RefundTimeoutJob: Exception while processing stale refund {Id}.", refund.RefundId);
            }
        }
    }
}
