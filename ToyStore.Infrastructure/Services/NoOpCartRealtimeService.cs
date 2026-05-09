using ToyStore.Application.DTOs.Carts;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Fallback cart realtime service registered in projects that don't host SignalR.
/// </summary>
public class NoOpCartRealtimeService : ICartRealtimeService
{
    public Task PublishAsync(
        int accountId,
        string eventName,
        CartDto cart,
        string message,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
