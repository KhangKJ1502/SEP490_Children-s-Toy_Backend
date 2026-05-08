using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service kiem tra tinh trang he thong.
/// </summary>
public interface IHealthService
{
    Task<Result> CheckDatabaseAsync(CancellationToken cancellationToken = default);
}
