using ToyStore.Application.DTOs.Carts;

namespace ToyStore.Application.Interfaces.Services;

public interface ICartRealtimeService
{
    Task PublishAsync(
        int accountId,
        string eventName,
        CartDto cart,
        string message,
        object? payload = null,
        CancellationToken cancellationToken = default);
}
