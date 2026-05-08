using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services;

public class HealthService : IHealthService
{
    private readonly SEP490ToyStoreContext _context;
    private readonly ILogger<HealthService> _logger;

    public HealthService(SEP490ToyStoreContext context, ILogger<HealthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> CheckDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (canConnect)
            {
                return Result.Success();
            }

            return Result.Failure("SERVICE_UNAVAILABLE", "Database is not reachable.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database readiness check failed.");
            return Result.Failure("SERVICE_UNAVAILABLE", "Database is not reachable.");
        }
    }
}
