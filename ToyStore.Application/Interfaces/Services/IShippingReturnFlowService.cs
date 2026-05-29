using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Handles GHN delivery-fail and return-to-warehouse order lifecycle transitions.
/// Called from ShippingWebhookService within an existing DB transaction.
/// </summary>
public interface IShippingReturnFlowService
{
    Task<ShippingReturnFlowResult> ProcessActionAsync(
        ShippingWebhookAction action,
        Order order,
        ShippingProviderTransaction tx,
        string ghnStatus,
        DateTime now,
        CancellationToken cancellationToken = default);
}
