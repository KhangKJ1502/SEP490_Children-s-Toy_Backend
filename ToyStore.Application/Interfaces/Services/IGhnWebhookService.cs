using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.DTOs.Orders;

namespace ToyStore.Application.Interfaces.Services;

public interface IGhnWebhookService
{
    Task ProcessAsync(GhnWebhookPayload payload, CancellationToken cancellationToken = default);
}
