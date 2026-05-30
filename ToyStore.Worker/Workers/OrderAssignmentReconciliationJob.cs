using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Worker.Workers;

public class OrderAssignmentReconciliationJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OrderAssignmentReconciliationJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public OrderAssignmentReconciliationJob(IServiceProvider services, ILogger<OrderAssignmentReconciliationJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderAssignmentReconciliationJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrderAssignmentReconciliationJob error");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var assignmentService = scope.ServiceProvider.GetRequiredService<IShiftAssignmentService>();

        var orderIds = await unitOfWork.OrderAssignments.GetOrdersMissingFullAssignmentAsync(ct);
        if (orderIds.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Reconciliation: retrying auto-assign for {Count} orders", orderIds.Count);

        foreach (var orderId in orderIds)
        {
            var result = await assignmentService.AutoAssignOrderAsync(orderId, ct);
            if (result.IsSuccess && result.Data is not null)
            {
                _logger.LogInformation(
                    "Reconciliation order {OrderId}: {Outcome}",
                    orderId,
                    result.Data.Result);
            }
        }
    }
}
